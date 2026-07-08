### Authored by Sitka Technology Group, 2020

import sys
import os

print ("starting")

from qgis.core import (
     QgsApplication, 
     QgsProcessingFeedback, 
     QgsVectorLayer,
     QgsFeatureRequest,
     QgsDataSourceUri,
     QgsFeature,
     QgsVectorFileWriter,
     QgsCoordinateReferenceSystem
)
from qgis.analysis import QgsNativeAlgorithms

print ("imported Qgis")
import processing
from processing.tools import dataobjects
from processing.core.Processing import Processing

import argparse

from pyqgis_utils import (
    duplicateLayer,
    raiseIfLayerInvalid,
    bufferZero,
    fixGeometriesWithinLayer,
    snapGeometriesWithinLayer,
    union,
    dissolve,
    QgisError,
    fetchLayerFromFileSystem,
    intersection,
    bufferSnapFix,
    clip,
    snapGeometriesToLayer,
    multipartToSinglePart,
    writeVectorLayerToDisk,
    selectByLocation,
    saveSelectedFeatures
)

JOIN_PREFIX = "Joined_"
OUTPUT_FOLDER = "OUTPUT FOLDER ERROR"
OUTPUT_FILE_PREFIX = "OUTPUT FILE PREFIX ERROR"
LGU_INPUT_PATH = "LGU_INPUT_PATH ERROR"
MODEL_BASIN_INPUT_PATH = "MODEL_BASIN_INPUT_PATH ERROR"
REGIONAL_SUBBASIN_INPUT_PATH = "REGIONAL_SUBBASIN_INPUT_PATH ERROR"
WQMP_INPUT_PATH = "WQMP_INPUT_PATH ERROR"
FILTER_PATH = None
RSB_IDs = None

def parseArguments():
    parser = argparse.ArgumentParser(description='Test PyQGIS connections to MSSQL')
    parser.add_argument('output_folder', type=str, help='The folder to write the final output to.')
    parser.add_argument('output_file_prefix', type=str, help='The filename prefix to write the final output to.')
    parser.add_argument('lgu_input_path', type=str, help='the path to the lgu input.')
    parser.add_argument('model_basin_input_path', type=str, help='The path to the model basin input.')
    parser.add_argument('regional_subbain_input_path', type=str, help='the path to the RSB input.')
    parser.add_argument('wqmp_input_path', type=str, help='The path to the WQMP input.')
    parser.add_argument('--rsb_ids', type=str, help='If present, filters the rsb layer down to only rsbs whose id is present in the list. Should be numbers separated by commas')
    parser.add_argument('--filter', type=str, help='The path to a geojson file containing the shape to filter inputs to through intersection')
    args = parser.parse_args()

    # this is easier to write than anything sane
    global LGU_INPUT_PATH
    global MODEL_BASIN_INPUT_PATH
    global REGIONAL_SUBBASIN_INPUT_PATH
    global WQMP_INPUT_PATH
    global OUTPUT_FOLDER
    global OUTPUT_FILE_PREFIX
    global OUTPUT_FOLDER_AND_FILE_PREFIX
    global FILTER_PATH
    OUTPUT_FOLDER = args.output_folder
    OUTPUT_FILE_PREFIX = args.output_file_prefix
    OUTPUT_FOLDER_AND_FILE_PREFIX = os.path.join(OUTPUT_FOLDER, OUTPUT_FILE_PREFIX)
    LGU_INPUT_PATH = args.lgu_input_path
    MODEL_BASIN_INPUT_PATH = args.model_basin_input_path
    REGIONAL_SUBBASIN_INPUT_PATH = args.regional_subbain_input_path
    WQMP_INPUT_PATH = args.wqmp_input_path

    if args.filter:
        FILTER_PATH = args.filter
        print(FILTER_PATH)

    if args.rsb_ids:
        RSB_IDs = args.rsb_ids
        print(RSB_IDs)

if __name__ == '__main__':
    parseArguments()
    
    qgs = QgsApplication([], False, "")
    qgs.initQgis()
    
    Processing.initialize()
    #QgsApplication.processingRegistry().addProvider(QgsNativeAlgorithms())

    # must set processing framework to skip invalid geometries as it defaults to halt-and-catch-fire
    PROCESSING_CONTEXT = dataobjects.createContext()
    PROCESSING_CONTEXT.setInvalidGeometryCheck(QgsFeatureRequest.GeometrySkipInvalid)

    delineationLayer = None

    delineationLayer_buffersnapfixpath = OUTPUT_FOLDER_AND_FILE_PREFIX + 'delineationLayer_buffersnapfix.geojson'
    delineationLayer = fetchLayerFromFileSystem(LGU_INPUT_PATH, "LGU Input")
    delineationLayerResult = bufferSnapFix(delineationLayer, delineationLayer_buffersnapfixpath, context=PROCESSING_CONTEXT)

    modelBasinLayer_buffersnapfixpath = OUTPUT_FOLDER_AND_FILE_PREFIX + 'modelBasinLayer_buffersnapfix.geojson'
    modelBasinLayer = fetchLayerFromFileSystem(MODEL_BASIN_INPUT_PATH, "Model Basin")
    modelBasinLayerResult = bufferSnapFix(modelBasinLayer, modelBasinLayer_buffersnapfixpath, context=PROCESSING_CONTEXT)

    regionalSubbasinLayer_buffersnapfixpath = OUTPUT_FOLDER_AND_FILE_PREFIX + 'regionalSubbasinLayer_buffersnapfix.geojson'
    regionalSubbasinLayer = fetchLayerFromFileSystem(REGIONAL_SUBBASIN_INPUT_PATH, "RSB")
    regionalSubbasinLayerResult = bufferSnapFix(regionalSubbasinLayer, regionalSubbasinLayer_buffersnapfixpath, context=PROCESSING_CONTEXT)

    wqmpLayer_buffersnapfixpath = OUTPUT_FOLDER_AND_FILE_PREFIX + 'wqmpLayer_buffersnapfix.geojson'
    wqmpLayer = fetchLayerFromFileSystem(WQMP_INPUT_PATH, "WQMP")
    wqmpLayerResult = bufferSnapFix(wqmpLayer, wqmpLayer_buffersnapfixpath, context=PROCESSING_CONTEXT)

    if RSB_IDs is not None:
        #If we've got set RSBs we want only what's within those RSBs'
        regionalSubbasinLayerClipped = clip(regionalSubbasinLayer_buffersnapfixpath, regionalSubbasinLayer_buffersnapfixpath, "RSBClipped", context=PROCESSING_CONTEXT)
        delineationLayerClipped = clip(delineationLayer_buffersnapfixpath, regionalSubbasinLayer_buffersnapfixpath, "DelineationClipped", context=PROCESSING_CONTEXT)
        wqmpLayerClipped = clip(wqmpLayer_buffersnapfixpath, regionalSubbasinLayer_buffersnapfixpath, "WQMPClipped", context=PROCESSING_CONTEXT)
    else:
        # At present time, we're only concerned with the area covered by Model basins. 
        regionalSubbasinLayerClipped = clip(regionalSubbasinLayer_buffersnapfixpath, modelBasinLayer_buffersnapfixpath, "RSBClipped", context=PROCESSING_CONTEXT)
        delineationLayerClipped = clip(delineationLayer_buffersnapfixpath, modelBasinLayer_buffersnapfixpath, "DelineationClipped", context=PROCESSING_CONTEXT)
        wqmpLayerClipped = clip(wqmpLayer_buffersnapfixpath, modelBasinLayer_buffersnapfixpath, "WQMPClipped", context=PROCESSING_CONTEXT)

    wqmpLayerClipped = bufferZero(wqmpLayerClipped, "WQMPClippedZero", None, context=PROCESSING_CONTEXT)

    rsb_wqmp = union(regionalSubbasinLayerClipped, wqmpLayerClipped, "rsb_wqmp", None, context=PROCESSING_CONTEXT)
    #raiseIfLayerInvalid(lspc_rsb_delineation)
    rsb_wqmp = bufferZero(rsb_wqmp, "ModelBasin-RSB-D", None, context=PROCESSING_CONTEXT)

    masterOverlay = union(rsb_wqmp, delineationLayerClipped, "MasterOverlay", None, context=PROCESSING_CONTEXT)

     # Filter down the masterLayer so that any geometries that overlap our area of concern are included in their entirety
    if FILTER_PATH is not None:
        filterLayer_dissolvedpath = OUTPUT_FOLDER_AND_FILE_PREFIX + 'filterLayer_dissolved.geojson'
        filterLayer = fetchLayerFromFileSystem(FILTER_PATH, "LGU Clip")
        filterLayerResult = dissolve(filterLayer, filesystemOutputPath=filterLayer_dissolvedpath, context=PROCESSING_CONTEXT)
        masterOverlay = selectByLocation(masterOverlay, filterLayer_dissolvedpath, "MasterOverlayFiltered", context=PROCESSING_CONTEXT)
        masterOverlay = saveSelectedFeatures(masterOverlay, "MasterOverlayFinal", None, context=PROCESSING_CONTEXT)

    masterOverlay = multipartToSinglePart(masterOverlay, "SinglePartLGUs", None, context=PROCESSING_CONTEXT)

    masterOverlay.startEditing()

    for feat in masterOverlay.getFeatures():
        ## todo: would be nice to also exclude those where the RegionalSubbasinID is non-exist. could also handle that by making ModelBasin_RSB as an intersect instead of a union.
        if feat.geometry().area() < 1 or feat["RSBID"] is None:
            masterOverlay.deleteFeature(feat.id())
    
    masterOverlay.commitChanges()

    finalOutputPath = OUTPUT_FOLDER_AND_FILE_PREFIX + '.geojson'
    writeVectorLayerToDisk(masterOverlay, finalOutputPath, "GeoJSON")

    #raiseIfLayerInvalid(masterOverlay)