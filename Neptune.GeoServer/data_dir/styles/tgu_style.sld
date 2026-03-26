<?xml version="1.0" encoding="ISO-8859-1"?>
<StyledLayerDescriptor version="1.0.0"  xsi:schemaLocation="http://www.opengis.net/sld StyledLayerDescriptor.xsd"  xmlns="http://www.opengis.net/sld"  xmlns:ogc="http://www.opengis.net/ogc"  xmlns:xlink="http://www.w3.org/1999/xlink"  xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <NamedLayer>
    <Name>Trash Generating Units</Name>
    <UserStyle>
      <Title>Trash Generating Units</Title>
      <FeatureTypeStyle>
        <Rule>
          <Name>PluFullTrashCapturePolygon</Name>
          <Title>PLU: Full Trash Capture Polygon</Title>
          <ogc:Filter>
            <ogc:And>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>IsPriorityLandUse</ogc:PropertyName>
                <ogc:Literal>1</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                <ogc:Literal>Full</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:Function name="in2">
                  <ogc:Function name="geometryType">
                    <ogc:PropertyName>TrashGeneratingUnitGeometry</ogc:PropertyName>
                  </ogc:Function>
                  <ogc:Literal>Polygon</ogc:Literal>
                  <ogc:Literal>MultiPolygon</ogc:Literal>
                </ogc:Function>
                <ogc:Literal>true</ogc:Literal>
              </ogc:PropertyIsEqualTo>
            </ogc:And>
          </ogc:Filter>
          <PolygonSymbolizer>
            <Fill>
              <CssParameter name="fill">#006400</CssParameter>
              <CssParameter name="fill-opacity">.8</CssParameter>
            </Fill>
          </PolygonSymbolizer>
        </Rule>
        <Rule>
          <Name>PluPartialTrashCapturePolygon</Name>
          <Title>PLU: Partial Capture Polygon</Title>
          <ogc:Filter>
            <ogc:And>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>IsPriorityLandUse</ogc:PropertyName>
                <ogc:Literal>1</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                <ogc:Literal>Partial</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:Function name="in2">
                  <ogc:Function name="geometryType">
                    <ogc:PropertyName>TrashGeneratingUnitGeometry</ogc:PropertyName>
                  </ogc:Function>
                  <ogc:Literal>Polygon</ogc:Literal>
                  <ogc:Literal>MultiPolygon</ogc:Literal>
                </ogc:Function>
                <ogc:Literal>true</ogc:Literal>
              </ogc:PropertyIsEqualTo>
            </ogc:And>
          </ogc:Filter>
          <PolygonSymbolizer>
            <Fill>
              <CssParameter name="fill">#b3ffbb</CssParameter>
              <CssParameter name="fill-opacity">.8</CssParameter>
            </Fill>
          </PolygonSymbolizer>
        </Rule>
        <Rule>
          <Name>PluScoreAPolygon</Name>
          <Title>PLU: OVTA Score A Polygon</Title>
          <ogc:Filter>
            <ogc:And>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>IsPriorityLandUse</ogc:PropertyName>
                <ogc:Literal>1</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsNotEqualTo>
                <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                <ogc:Literal>Full</ogc:Literal>
              </ogc:PropertyIsNotEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>AssessmentScore</ogc:PropertyName>
                <ogc:Literal>A</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:Function name="in2">
                  <ogc:Function name="geometryType">
                    <ogc:PropertyName>TrashGeneratingUnitGeometry</ogc:PropertyName>
                  </ogc:Function>
                  <ogc:Literal>Polygon</ogc:Literal>
                  <ogc:Literal>MultiPolygon</ogc:Literal>
                </ogc:Function>
                <ogc:Literal>true</ogc:Literal>
              </ogc:PropertyIsEqualTo>
            </ogc:And>
          </ogc:Filter>
          <PolygonSymbolizer>
            <Fill>
              <CssParameter name="fill">#5ce600</CssParameter>
              <CssParameter name="fill-opacity">.7</CssParameter>
            </Fill>
          </PolygonSymbolizer>
        </Rule>
        <Rule>
          <Name>PluUntreatedPolygon</Name>
          <Title>PLU: Untreated Polygon</Title>
          <ogc:Filter>
            <ogc:And>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>IsPriorityLandUse</ogc:PropertyName>
                <ogc:Literal>1</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:Or>
                <ogc:PropertyIsEqualTo>
                  <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                  <ogc:Literal>None</ogc:Literal>
                </ogc:PropertyIsEqualTo>
                <ogc:PropertyIsEqualTo>
                  <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                  <ogc:Literal>NotProvided</ogc:Literal>
                </ogc:PropertyIsEqualTo>
              </ogc:Or>
              <ogc:PropertyIsNotEqualTo>
                <ogc:PropertyName>AssessmentScore</ogc:PropertyName>
                <ogc:Literal>A</ogc:Literal>
              </ogc:PropertyIsNotEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:Function name="in2">
                  <ogc:Function name="geometryType">
                    <ogc:PropertyName>TrashGeneratingUnitGeometry</ogc:PropertyName>
                  </ogc:Function>
                  <ogc:Literal>Polygon</ogc:Literal>
                  <ogc:Literal>MultiPolygon</ogc:Literal>
                </ogc:Function>
                <ogc:Literal>true</ogc:Literal>
              </ogc:PropertyIsEqualTo>
            </ogc:And>
          </ogc:Filter>
          <PolygonSymbolizer>
            <Fill>
              <CssParameter name="fill">#E487FF</CssParameter>
              <CssParameter name="fill-opacity">.7</CssParameter>
            </Fill>
          </PolygonSymbolizer>
        </Rule>
        <Rule>
          <Name>AluFullTrashCapturePolygon</Name>
          <Title>ALU: Full Trash Capture Polygon</Title>
          <ogc:Filter>
            <ogc:And>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>IsPriorityLandUse</ogc:PropertyName>
                <ogc:Literal>0</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                <ogc:Literal>Full</ogc:Literal>
              </ogc:PropertyIsEqualTo> 
              <ogc:PropertyIsEqualTo>
                <ogc:Function name="in2">
                  <ogc:Function name="geometryType">
                    <ogc:PropertyName>TrashGeneratingUnitGeometry</ogc:PropertyName>
                  </ogc:Function>
                  <ogc:Literal>Polygon</ogc:Literal>
                  <ogc:Literal>MultiPolygon</ogc:Literal>
                </ogc:Function>
                <ogc:Literal>true</ogc:Literal>
              </ogc:PropertyIsEqualTo>
            </ogc:And>
          </ogc:Filter>
          <PolygonSymbolizer>
            <Fill>
              <CssParameter name="fill">#2171B5</CssParameter>
              <CssParameter name="fill-opacity">.7</CssParameter>
            </Fill>
          </PolygonSymbolizer>
        </Rule>
        <Rule>
          <Name>AluPartialTrashCapturePolygon</Name>
          <Title>ALU: Partial Trash Capture Polygon</Title>
          <ogc:Filter>
            <ogc:And>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>IsPriorityLandUse</ogc:PropertyName>
                <ogc:Literal>0</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                <ogc:Literal>Partial</ogc:Literal>
              </ogc:PropertyIsEqualTo> 
              <ogc:PropertyIsEqualTo>
                <ogc:Function name="in2">
                  <ogc:Function name="geometryType">
                    <ogc:PropertyName>TrashGeneratingUnitGeometry</ogc:PropertyName>
                  </ogc:Function>
                  <ogc:Literal>Polygon</ogc:Literal>
                  <ogc:Literal>MultiPolygon</ogc:Literal>
                </ogc:Function>
                <ogc:Literal>true</ogc:Literal>
              </ogc:PropertyIsEqualTo>
            </ogc:And>
          </ogc:Filter>
          <PolygonSymbolizer>
            <Fill>
              <CssParameter name="fill">#d2b48c</CssParameter>
              <CssParameter name="fill-opacity">.7</CssParameter>
            </Fill>
          </PolygonSymbolizer>
        </Rule>
        <Rule>
          <Name>AluUntreatedPolygon</Name>
          <Title>ALU: Untreated Polygon</Title>
          <ogc:Filter>
            <ogc:And>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>IsPriorityLandUse</ogc:PropertyName>
                <ogc:Literal>0</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsNotEqualTo>
                <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                <ogc:Literal>Full</ogc:Literal>
              </ogc:PropertyIsNotEqualTo>
              <ogc:PropertyIsNotEqualTo>
                <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                <ogc:Literal>Partial</ogc:Literal>
              </ogc:PropertyIsNotEqualTo> 
              <ogc:PropertyIsEqualTo>
                <ogc:Function name="in2">
                  <ogc:Function name="geometryType">
                    <ogc:PropertyName>TrashGeneratingUnitGeometry</ogc:PropertyName>
                  </ogc:Function>
                  <ogc:Literal>Polygon</ogc:Literal>
                  <ogc:Literal>MultiPolygon</ogc:Literal>
                </ogc:Function>
                <ogc:Literal>true</ogc:Literal>
              </ogc:PropertyIsEqualTo>
            </ogc:And>
          </ogc:Filter>
          <PolygonSymbolizer>
            <Fill>
              <CssParameter name="fill">#d2d2d2</CssParameter>
              <CssParameter name="fill-opacity">.7</CssParameter>
            </Fill>
          </PolygonSymbolizer>
        </Rule>
        <VendorOption name="sortBy">IsPriorityLandUse, TrashCaptureStatusSortOrder D, AssessmentScore D</VendorOption>
      </FeatureTypeStyle>
      <FeatureTypeStyle>
        <Rule>
          <Name>PluFullTrashCaptureLine</Name>
          <Title>PLU: Full Trash Capture Line</Title>
          <ogc:Filter>
            <ogc:And>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>IsPriorityLandUse</ogc:PropertyName>
                <ogc:Literal>1</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                <ogc:Literal>Full</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:Function name="in3">
                  <ogc:Function name="geometryType">
                    <ogc:PropertyName>TrashGeneratingUnitGeometry</ogc:PropertyName>
                  </ogc:Function>
                  <ogc:Literal>LineString</ogc:Literal>
                  <ogc:Literal>LinearRing</ogc:Literal>
                  <ogc:Literal>MultiLineString</ogc:Literal>
                </ogc:Function>
                <ogc:Literal>true</ogc:Literal>
              </ogc:PropertyIsEqualTo>              
            </ogc:And>
          </ogc:Filter>
          <LineSymbolizer>
            <Stroke>
              <CssParameter name="stroke">
                <ogc:Literal>#006400</ogc:Literal>
              </CssParameter>
              <CssParameter name="stroke-width">
                <ogc:Literal>2</ogc:Literal>
              </CssParameter>
            </Stroke>
          </LineSymbolizer>
        </Rule>
        <Rule>
          <Name>PluPartialTrashCaptureLine</Name>
          <Title>PLU: Partial Capture Line</Title>
          <ogc:Filter>
            <ogc:And>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>IsPriorityLandUse</ogc:PropertyName>
                <ogc:Literal>1</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                <ogc:Literal>Partial</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:Function name="in3">
                  <ogc:Function name="geometryType">
                    <ogc:PropertyName>TrashGeneratingUnitGeometry</ogc:PropertyName>
                  </ogc:Function>
                  <ogc:Literal>LineString</ogc:Literal>
                  <ogc:Literal>LinearRing</ogc:Literal>
                  <ogc:Literal>MultiLineString</ogc:Literal>
                </ogc:Function>
                <ogc:Literal>true</ogc:Literal>
              </ogc:PropertyIsEqualTo>              
            </ogc:And>
          </ogc:Filter>
          <LineSymbolizer>
            <Stroke>
              <CssParameter name="stroke">
                <ogc:Literal>#b3ffbb</ogc:Literal>
              </CssParameter>
              <CssParameter name="stroke-width">
                <ogc:Literal>2</ogc:Literal>
              </CssParameter>
            </Stroke>
          </LineSymbolizer>
        </Rule>
        <Rule>
          <Name>PluScoreALine</Name>
          <Title>PLU: OVTA Score A Line</Title>
          <ogc:Filter>
            <ogc:And>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>IsPriorityLandUse</ogc:PropertyName>
                <ogc:Literal>1</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsNotEqualTo>
                <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                <ogc:Literal>Full</ogc:Literal>
              </ogc:PropertyIsNotEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>AssessmentScore</ogc:PropertyName>
                <ogc:Literal>A</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:Function name="in3">
                  <ogc:Function name="geometryType">
                    <ogc:PropertyName>TrashGeneratingUnitGeometry</ogc:PropertyName>
                  </ogc:Function>
                  <ogc:Literal>LineString</ogc:Literal>
                  <ogc:Literal>LinearRing</ogc:Literal>
                  <ogc:Literal>MultiLineString</ogc:Literal>
                </ogc:Function>
                <ogc:Literal>true</ogc:Literal>
              </ogc:PropertyIsEqualTo>              
            </ogc:And>
          </ogc:Filter>
          <LineSymbolizer>
            <Stroke>
              <CssParameter name="stroke">
                <ogc:Literal>#5ce600</ogc:Literal>
              </CssParameter>
              <CssParameter name="stroke-width">
                <ogc:Literal>2</ogc:Literal>
              </CssParameter>
            </Stroke>
          </LineSymbolizer>
        </Rule>
        <Rule>
          <Name>PluUntreatedLine</Name>
          <Title>PLU: Untreated Line</Title>
          <ogc:Filter>
            <ogc:And>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>IsPriorityLandUse</ogc:PropertyName>
                <ogc:Literal>1</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:Or>
                <ogc:PropertyIsEqualTo>
                  <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                  <ogc:Literal>None</ogc:Literal>
                </ogc:PropertyIsEqualTo>
                <ogc:PropertyIsEqualTo>
                  <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                  <ogc:Literal>NotProvided</ogc:Literal>
                </ogc:PropertyIsEqualTo>
              </ogc:Or>
              <ogc:PropertyIsNotEqualTo>
                <ogc:PropertyName>AssessmentScore</ogc:PropertyName>
                <ogc:Literal>A</ogc:Literal>
              </ogc:PropertyIsNotEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:Function name="in3">
                  <ogc:Function name="geometryType">
                    <ogc:PropertyName>TrashGeneratingUnitGeometry</ogc:PropertyName>
                  </ogc:Function>
                  <ogc:Literal>LineString</ogc:Literal>
                  <ogc:Literal>LinearRing</ogc:Literal>
                  <ogc:Literal>MultiLineString</ogc:Literal>
                </ogc:Function>
                <ogc:Literal>true</ogc:Literal>
              </ogc:PropertyIsEqualTo>              
            </ogc:And>
          </ogc:Filter>
          <LineSymbolizer>
            <Stroke>
              <CssParameter name="stroke">
                <ogc:Literal>#ecabff</ogc:Literal>
              </CssParameter>
              <CssParameter name="stroke-width">
                <ogc:Literal>2</ogc:Literal>
              </CssParameter>
            </Stroke>
          </LineSymbolizer>
        </Rule>
        <Rule>
          <Name>AluFullTrashCaptureLine</Name>
          <Title>ALU: Full Trash Capture Line</Title>
          <ogc:Filter>
            <ogc:And>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>IsPriorityLandUse</ogc:PropertyName>
                <ogc:Literal>0</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                <ogc:Literal>Full</ogc:Literal>
              </ogc:PropertyIsEqualTo> 
              <ogc:PropertyIsEqualTo>
                <ogc:Function name="in3">
                  <ogc:Function name="geometryType">
                    <ogc:PropertyName>TrashGeneratingUnitGeometry</ogc:PropertyName>
                  </ogc:Function>
                  <ogc:Literal>LineString</ogc:Literal>
                  <ogc:Literal>LinearRing</ogc:Literal>
                  <ogc:Literal>MultiLineString</ogc:Literal>
                </ogc:Function>
                <ogc:Literal>true</ogc:Literal>
              </ogc:PropertyIsEqualTo>              
            </ogc:And>
          </ogc:Filter>
          <LineSymbolizer>
            <Stroke>
              <CssParameter name="stroke">
                <ogc:Literal>#2171B5</ogc:Literal>
              </CssParameter>
              <CssParameter name="stroke-width">
                <ogc:Literal>2</ogc:Literal>
              </CssParameter>
            </Stroke>
          </LineSymbolizer>
        </Rule>
        <Rule>
          <Name>AluPartialTrashCaptureLine</Name>
          <Title>ALU: Partial Trash Capture Line</Title>
          <ogc:Filter>
            <ogc:And>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>IsPriorityLandUse</ogc:PropertyName>
                <ogc:Literal>0</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                <ogc:Literal>Partial</ogc:Literal>
              </ogc:PropertyIsEqualTo> 
              <ogc:PropertyIsEqualTo>
                <ogc:Function name="in3">
                  <ogc:Function name="geometryType">
                    <ogc:PropertyName>TrashGeneratingUnitGeometry</ogc:PropertyName>
                  </ogc:Function>
                  <ogc:Literal>LineString</ogc:Literal>
                  <ogc:Literal>LinearRing</ogc:Literal>
                  <ogc:Literal>MultiLineString</ogc:Literal>
                </ogc:Function>
                <ogc:Literal>true</ogc:Literal>
              </ogc:PropertyIsEqualTo>              
            </ogc:And>
          </ogc:Filter>
          <LineSymbolizer>
            <Stroke>
              <CssParameter name="stroke">
                <ogc:Literal>#d2b48c</ogc:Literal>
              </CssParameter>
              <CssParameter name="stroke-width">
                <ogc:Literal>2</ogc:Literal>
              </CssParameter>
            </Stroke>
          </LineSymbolizer>
        </Rule>
        <Rule>
          <Name>AluUntreatedLine</Name>
          <Title>ALU: Untreated Line</Title>
          <ogc:Filter>
            <ogc:And>
              <ogc:PropertyIsEqualTo>
                <ogc:PropertyName>IsPriorityLandUse</ogc:PropertyName>
                <ogc:Literal>0</ogc:Literal>
              </ogc:PropertyIsEqualTo>
              <ogc:PropertyIsNotEqualTo>
                <ogc:PropertyName>TrashCaptureStatus</ogc:PropertyName>
                <ogc:Literal>Full</ogc:Literal>
              </ogc:PropertyIsNotEqualTo> 
              <ogc:PropertyIsEqualTo>
                <ogc:Function name="in3">
                  <ogc:Function name="geometryType">
                    <ogc:PropertyName>TrashGeneratingUnitGeometry</ogc:PropertyName>
                  </ogc:Function>
                  <ogc:Literal>LineString</ogc:Literal>
                  <ogc:Literal>LinearRing</ogc:Literal>
                  <ogc:Literal>MultiLineString</ogc:Literal>
                </ogc:Function>
                <ogc:Literal>true</ogc:Literal>
              </ogc:PropertyIsEqualTo>              
            </ogc:And>
          </ogc:Filter>
          <LineSymbolizer>
            <Stroke>
              <CssParameter name="stroke">
                <ogc:Literal>#d2d2d2</ogc:Literal>
              </CssParameter>
              <CssParameter name="stroke-width">
                <ogc:Literal>2</ogc:Literal>
              </CssParameter>
            </Stroke>
          </LineSymbolizer>
        </Rule>
        <VendorOption name="sortBy">IsPriorityLandUse, TrashCaptureStatusSortOrder D, AssessmentScore D</VendorOption>
      </FeatureTypeStyle>
    </UserStyle>
  </NamedLayer>
</StyledLayerDescriptor>