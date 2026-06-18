using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Neptune.Common.JsonConverters;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.IO.Converters;

namespace Neptune.Common.GeoSpatial;

public static class GeoJsonSerializer
{
    public static JsonSerializerOptions DefaultSerializerOptions = CreateGeoJSONSerializerOptions();

    public static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, DefaultSerializerOptions);
    }

    public static async Task<T> DeserializeAsync<T>(Stream stream)
    {
        return await JsonSerializer.DeserializeAsync<T>(stream, DefaultSerializerOptions);
    }

    public static string Serialize(object value)
    {
        return JsonSerializer.Serialize(value, DefaultSerializerOptions);
    }

    public static async Task<FeatureCollection> GetFeatureCollectionFromGeoJsonByteArray(byte[] fileContentsByteArray, JsonSerializerOptions jsonSerializerOptions)
    {
        await using var memoryStream = new MemoryStream(fileContentsByteArray);
        return await GetFeatureCollectionFromGeoJsonStream(memoryStream, jsonSerializerOptions);
    }

    public static async Task<FeatureCollection> GetFeatureCollectionFromGeoJsonStream(Stream stream, JsonSerializerOptions jsonSerializerOptions)
    {
        return await JsonSerializer.DeserializeAsync<FeatureCollection>(stream, jsonSerializerOptions);
    }

    public static JsonSerializerOptions CreateGeoJSONSerializerOptions()
    {
        return CreateGeoJSONSerializerOptions(6, 10);
    }

    public static JsonSerializerOptions CreateGeoJSONSerializerOptions(int numberOfSignificantDigits)
    {
        return CreateGeoJSONSerializerOptions(6, numberOfSignificantDigits);
    }

    public static JsonSerializerOptions CreateGeoJSONSerializerOptions(int coordinatePrecision, int numberOfSignificantDigits)
    {
        var jsonSerializerOptions = CreateDefaultJSONSerializerOptions(numberOfSignificantDigits);
        //var scale = Math.Pow(10, coordinatePrecision);
        //var geometryFactory = new GeometryFactory(new PrecisionModel(scale));
        jsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory(false));
        return jsonSerializerOptions;
    }

    public static JsonSerializerOptions CreateDefaultJSONSerializerOptions(int numberOfSignificantDigits)
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = null,
        };
        jsonSerializerOptions.Converters.Add(new DateTimeConverter());
        jsonSerializerOptions.Converters.Add(new NullableDateTimeConverter());
        jsonSerializerOptions.Converters.Add(new DoubleConverter(numberOfSignificantDigits));
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        return jsonSerializerOptions;
    }

    public static async Task SerializeToStream<T>(T objectToSerialize, JsonSerializerOptions jsonSerializerOptions, MemoryStream stream)
    {
        await JsonSerializer.SerializeAsync(stream, objectToSerialize, jsonSerializerOptions);
    }

    public static byte[] WriteFeaturesToByteArray(IEnumerable<IFeature> features, JsonSerializerOptions jsonSerializerOptions)
    {
        var featureCollection = new FeatureCollection();
        foreach (var feature in features)
        {
            featureCollection.Add(feature);
        }

        return SerializeToByteArray(featureCollection, jsonSerializerOptions);
    }

    public static byte[] SerializeToByteArray<T>(T objectToSerialize, JsonSerializerOptions jsonSerializerOptions)
    {
        return JsonSerializer.SerializeToUtf8Bytes(objectToSerialize, jsonSerializerOptions);
    }

    public static T DeserializeFromFeature<T>(IFeature feature, JsonSerializerOptions geoJSONSerializerOptions) where T : IHasGeometry
    {
        ((IPartiallyDeserializedAttributesTable)feature.Attributes).TryDeserializeJsonObject<T>(geoJSONSerializerOptions, out var deserialized);
        deserialized.Geometry = feature.Geometry;
        return deserialized;
    }

    public static T DeserializeFromFeatureWithCCWCheck<T>(IFeature feature, JsonSerializerOptions geoJSONSerializerOptions, int srid) where T : IHasGeometry
    {
        ((IPartiallyDeserializedAttributesTable)feature.Attributes).TryDeserializeJsonObject<T>(geoJSONSerializerOptions, out var deserialized);
        var geometry = feature.Geometry.MakeValid();
        if (geometry.GeometryType.ToUpper() == "POLYGON")
        {
            var polygon = (Polygon)geometry;
            if (!polygon.Shell.IsCCW)
            {
                geometry = geometry.Reverse();
            }
        }
        else if (geometry.GeometryType.ToUpper() == "MULTIPOLYGON")
        {
            if (geometry.NumGeometries == 1)
            {
                var geometryPart = (Polygon)geometry.GetGeometryN(0);
                if (!geometryPart.Shell.IsCCW)
                {
                    geometry = geometryPart.Reverse();
                }
            }
            else
            {
                for (var i = 0; i < geometry.NumGeometries; i++)
                {
                    var geometryPart = (Polygon)geometry.GetGeometryN(i);
                    if (!geometryPart.Shell.IsCCW)
                    {
                        // if any is not counter-clockwise, just reverse the whole geometry and stop processing the rest
                        geometry = geometry.Reverse();
                        break;
                    }
                }
            }
        }
        deserialized.Geometry = geometry;
        deserialized.Geometry.SRID = srid;
        return deserialized;
    }

    public static async Task<List<T>> DeserializeFromFeatureCollectionWithCCWCheck<T>(byte[] byteArray, JsonSerializerOptions geoJSONSerializerOptions, int srid) where T : IHasGeometry
    {
        var featureCollection = await GetFeatureCollectionFromGeoJsonByteArray(byteArray, geoJSONSerializerOptions);
        return DeserializeFromFeatureCollectionWithCCWCheck<T>(featureCollection, geoJSONSerializerOptions, srid);
    }

    public static List<T> DeserializeFromFeatureCollection<T>(FeatureCollection featureCollection, JsonSerializerOptions geoJSONSerializerOptions) where T : IHasGeometry
    {
        return featureCollection.AsParallel().Select(x => DeserializeFromFeature<T>(x, geoJSONSerializerOptions)).ToList();
    }

    public static List<T> DeserializeFromFeatureCollectionWithCCWCheck<T>(FeatureCollection featureCollection, JsonSerializerOptions geoJSONSerializerOptions, int srid) where T : IHasGeometry
    {
        return featureCollection.AsParallel().Select(x => DeserializeFromFeatureWithCCWCheck<T>(x, geoJSONSerializerOptions, srid)).ToList();
    }

    public static List<T> DeserializeFromFeatureCollection<T>(FeatureCollection featureCollection) where T : IHasGeometry
    {
        return featureCollection.AsParallel().Select(x => DeserializeFromFeature<T>(x, DefaultSerializerOptions)).ToList();
    }
}