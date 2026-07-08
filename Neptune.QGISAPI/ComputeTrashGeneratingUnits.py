### Authored by Nicholas Padinha for Sitka Technology Group, 2019
### A PyQGIS pipeline for topologically-correcting a polygon layer
### by replacing overlaps between features with new features.
### Attributes are assigned to the new features based on the overlapping
### features' attributes. In this case the determination is made
### based on a domain-specific rule.
### Consult the diagram for a conceptual overview of the process

import os
import sys

print ("starting")

from qgis.core import (
     QgsApplication, 
     QgsProcessingFeedback, 
     QgsVectorLayer,
     QgsFeatureRequest,
     QgsDataSourceUri,
     QgsFeature,
     QgsCoordinateReferenceSystem,
     QgsProject,
     QgsWkbTypes
)
from qgis.analysis import QgsNativeAlgorithms

print ("imported Qgis")

import processing
from processing.tools import dataobjects
from processing.core.Processing import Processing

import argparse

from pyqgis_utils import (
    duplicateLayer,
    fetchLayerFromFileSystem,
    raiseIfLayerInvalid,
    bufferSnapFix,
    removeSliversAndNulls,
    bufferZero,
    fixGeometriesWithinLayer,
    snapGeometriesWithinLayer,
    union,
    QgisError,
    writeVectorLayerToDisk,
    multipartToSinglePart
)

JOIN_PREFIX = "Joined_"
OUTPUT_FOLDER = "OUTPUT FOLDER ERROR"
OUTPUT_FILE_PREFIX = "OUTPUT FILE PREFIX ERROR"
TGU_INPUT_PATH = "TGU_INPUT_PATH ERROR"
OVTA_INPUT_PATH = "OVTA_INPUT_PATH ERROR"
WQMP_INPUT_PATH = "WQMP_INPUT_PATH ERROR"
LAND_USE_BLOCK_INPUT_PATH = "LAND_USE_BLOCK_INPUT_PATH ERROR"


def parseArguments():
    parser = argparse.ArgumentParser(description='Test PyQGIS connections to MSSQL')
    parser.add_argument('output_folder', metavar='d', type=str, help='The folder to write the final output to.')
    parser.add_argument('output_file_prefix', metavar='d', type=str, help='The filename prefix to write the final output to.')
    parser.add_argument('tgu_input_path', type=str, help='the path to the TGU input.')
    parser.add_argument('ovta_input_path', type=str, help='the path to the OVTA input.')
    parser.add_argument('wqmp_input_path', type=str, help='The path to the WQMP input.')
    parser.add_argument('land_use_block_input_path', type=str, help='The path to the Land Use Block input.')

    args = parser.parse_args()

    # this is easier to write than anything sane
    global OUTPUT_FOLDER
    global OUTPUT_FILE_PREFIX
    global OUTPUT_FOLDER_AND_FILE_PREFIX
    global TGU_INPUT_PATH
    global OVTA_INPUT_PATH
    global WQMP_INPUT_PATH
    global LAND_USE_BLOCK_INPUT_PATH
    OUTPUT_FOLDER = args.output_folder
    OUTPUT_FILE_PREFIX = args.output_file_prefix
    OUTPUT_FOLDER_AND_FILE_PREFIX = os.path.join(OUTPUT_FOLDER, OUTPUT_FILE_PREFIX)
    TGU_INPUT_PATH = args.tgu_input_path
    OVTA_INPUT_PATH = args.ovta_input_path
    WQMP_INPUT_PATH = args.wqmp_input_path
    LAND_USE_BLOCK_INPUT_PATH = args.land_use_block_input_path

def assignFieldsToLayerFromSourceLayer(target, source):
    target_layer_data = target.dataProvider()
    source_layer_data = source.dataProvider()

    attr = source_layer_data.fields().toList()
    target_layer_data.addAttributes(attr)
    target.updateFields()

def unionAndFix(inputLayer, overlayLayer, inputLayerOutputPath, overlayLayerOutputPath, unionResultOutputPath, context=None):
    inputLayer = bufferSnapFix(inputLayer, inputLayerOutputPath, context)
    overlayLayer = bufferSnapFix(overlayLayer, overlayLayerOutputPath, context)
    if unionResultOutputPath is not None:
        result = union(inputLayerOutputPath, overlayLayerOutputPath, None, unionResultOutputPath, context)
    else:
        result = union(inputLayerOutputPath, overlayLayerOutputPath, 'unioned', None, context)
    #selectPolygonFeatures(result, context)
    #saveSelectedFeatures(result, None, unionResultOutputPath, context)
    print('Union succeeded')
    return result

class Flatten:
    
    def __init__(self, working_layer, layer_identifier, compareFeaturesViaJoinedLayer, compareFeaturesViaSeparateLayers, fieldToRemove):
        
        # don't use the DB as the datasource
        self.working_layer = duplicateLayer(working_layer, "Candidate Layer")
        
        self.fields = self.working_layer.dataProvider().fields()
                
        self.layer_identifier = layer_identifier
        self.compareFeaturesViaJoinedLayer = compareFeaturesViaJoinedLayer
        self.compareFeaturesViaSeparateLayers = compareFeaturesViaSeparateLayers
        self.fieldToRemove = fieldToRemove
        
    def run(self):
        # (2)(a) Collapse equality chains
        self.removeEqualitiesFromCandidateLayer()
        # (2)(b) Deal with withins
        self.handleInclusionsInCandidateLayer()
        # (2)(c) Deal with ordinary overlaps
        self.handleOverlapsInCandidateLayer()
        # remove fieldToRemove
        self.removeFieldFromLayer()

    def removeFieldFromLayer(self):
        fieldIndex = self.fields.indexFromName(self.fieldToRemove)
        print("Removing field '" + self.fieldToRemove + "'")
        self.working_layer.dataProvider().deleteAttributes([fieldIndex])
        self.working_layer.updateFields()

    def removeEqualitiesFromCandidateLayer(self):
        print("Start collapse equality chains")
        print("Starting with {count} features".format(count = str(self.working_layer.featureCount())))

        dupe = duplicateLayer(self.working_layer, "Duplicate")
        join_prefix = JOIN_PREFIX

        res = processing.run("qgis:joinattributesbylocation", {
	        'INPUT': self.working_layer,
	        'JOIN': dupe,
	        'PREDICATE': ['2'], # 2 := Equals
	        'JOIN_FIELDS':'',
	        'METHOD':'0',
	        'DISCARD_NONMATCHING':False,
	        'PREFIX': join_prefix,
	        'OUTPUT':'memory:equalities'
        }, context=PROCESSING_CONTEXT)

        equalities_layer = res['OUTPUT']

        # equality is reflexive, so this filter is wlog
        filter_icl = QgsFeatureRequest()
        filter_icl.setFilterExpression(self.lessThanIDFilterString())
        
        self.working_layer.startEditing()

        # for the equality predicate, we remove the losers from all future consideration
        for feat in equalities_layer.getFeatures(filter_icl):
            if self.compareFeatures(feat):   # left side loses
                self.removeFromCandidateLayer(feat[self.getLayerIdentifier(False)])
            else:
                self.removeFromCandidateLayer(feat[self.getLayerIdentifier(True)])                     # right side loses

        self.working_layer.commitChanges()
        print("Ending with {count} features".format(count = str(self.working_layer.featureCount())))
    
    def handleInclusionsInCandidateLayer(self):
        print("Start handle inclusions")
        print("Starting with {count} features".format(count = str(self.working_layer.featureCount())))

        dupe = duplicateLayer(self.working_layer, "Duplicate")
        join_prefix = JOIN_PREFIX

        res = processing.run("qgis:joinattributesbylocation", {
	        'INPUT': self.working_layer,
	        'JOIN': dupe,
	        'PREDICATE': ['5'], # 5 := Within
	        'JOIN_FIELDS':'',
	        'METHOD':'0',
	        'DISCARD_NONMATCHING':False,
	        'PREFIX': join_prefix,
	        'OUTPUT':'memory:inclusions'
        }, context=PROCESSING_CONTEXT)

        inclusions_layer = res['OUTPUT']

        self.working_layer.startEditing()

        self.inclusion_count_this_iteration = 0

        # the smaller is removed if it loses; else it is retained for the next iteration
        for feat in inclusions_layer.getFeatures(self.neqIDFilterString()):
            self.inclusion_count_this_iteration += 1
            if self.compareFeatures(feat):   # smaller loses and is removed, never to be heard from again
                
                self.removeFromCandidateLayer(feat[self.getLayerIdentifier(False)])
            else:   
                # smaller (left) wins, subtract it from the larger (right) geometry
                # unless you're using FIDs (blech), QGIS can only supply a list of features to respond to a request. We know this is a unique identifier though
                # (In the previous version of the algorithm, this would not work because the candidate layer would eventually have multiple features per layer identifier.
                # In this version, we no longer create new features for the candidate layer)
                for lf in self.working_layer.getFeatures("{identifier} = {id}".format(identifier = self.layer_identifier, id=feat[self.getLayerIdentifier(False)])):
                    left_feat = lf

                for rf in self.working_layer.getFeatures("{identifier} = {id}".format(identifier = self.layer_identifier, id=feat[self.getLayerIdentifier(True)])):
                    right_feat = rf

                # create the new geometry from the difference
                new_right_feat_geom = right_feat.geometry().difference(left_feat.geometry())
                right_feat.setGeometry(new_right_feat_geom)
                    
                # copy the old feature and set the geometry on the new feature
                new_feat = QgsFeature(right_feat)
                new_feat.setGeometry(new_right_feat_geom)

                # delete the old feature and add the new
                self.working_layer.deleteFeature(right_feat.id())
                self.working_layer.addFeature(new_feat)

        self.working_layer.commitChanges()

        print("Ending with {count} features".format(count = str(self.working_layer.featureCount())))

    def handleOverlapsInCandidateLayer(self):
        print("Starting handle overlaps")
        print("Starting with {count} features".format(count = str(self.working_layer.featureCount())))

        dupe = duplicateLayer(self.working_layer, "Duplicate")

        join_prefix = JOIN_PREFIX

        res = processing.run("qgis:joinattributesbylocation", {
	        'INPUT':self.working_layer,
	        'JOIN':dupe,
	        'PREDICATE': ['4'], # 4 := Overlaps
	        'JOIN_FIELDS':'',
	        'METHOD':'0',
	        'DISCARD_NONMATCHING':False,
	        'PREFIX': join_prefix,
	        'OUTPUT':'memory:overlaps'
        }, context=PROCESSING_CONTEXT)

        intersect_contrib_layer = res['OUTPUT']

        
        self.overlap_count_this_iteration = 0
        # the overlap operation without withins is reflexive, so this filter is wlog
        for feat in intersect_contrib_layer.getFeatures(self.lessThanIDFilterString()):
            self.overlap_count_this_iteration += 1 
            self.working_layer.startEditing()
            # unless you're using FIDs (blech), QGIS can only supply a list of features to respond to a request. We know this is a unique identifier though
            # (In the previous version of the algorithm, this would not work because the candidate layer would eventually have multiple features per layer identifier.
            # In this version, we no longer create new features for the candidate layer)
            for lf in self.working_layer.getFeatures("{identifier} = {id}".format(identifier = self.layer_identifier, id=feat[self.getLayerIdentifier(False)])):
                left_feat = lf

            for rf in self.working_layer.getFeatures("{identifier} = {id}".format(identifier = self.layer_identifier, id=feat[self.getLayerIdentifier(True)])):
                right_feat = rf
            
            if left_feat.isValid() and right_feat.isValid():
                if self.compareFeatures(left_feat, right_feat):   # Left side loses. Assign Left = Left - Right
                    
                    # create the new geometry from the difference
                    new_left_feat_geom = left_feat.geometry().difference(right_feat.geometry())
                    
                    # copy the old feature and set the geometry on the new feature
                    new_feat = QgsFeature(left_feat)
                    new_feat.setGeometry(new_left_feat_geom)

#                    if(new_feat.geometry().type() != self.working_layer.geometryType()):
#                        print("Left won: " + str(left_feat[self.layer_identifier]) + " - " + QgsWkbTypes.geometryDisplayString(new_feat.geometry().type()) + str(new_feat.geometry().type()))
                    # delete the old feature and add the new
                    self.working_layer.deleteFeature(left_feat.id())
                    self.working_layer.addFeature(new_feat)
                else:                                             # Right side loses. Assign Right = Right - Left
                    # create the new geometry from the difference
                    new_right_feat_geom = right_feat.geometry().difference(left_feat.geometry())
                    
                    # copy the old feature and set the geometry on the new feature
                    new_feat = QgsFeature(right_feat)
                    new_feat.setGeometry(new_right_feat_geom)

#                   if(new_feat.geometry().type() != self.working_layer.geometryType()):
#                        print("Right won: " + str(right_feat[self.layer_identifier]) + " - " + QgsWkbTypes.geometryDisplayString(new_feat.geometry().type()) + str(new_feat.geometry().type()))
                    # delete the old feature and add the new
                    self.working_layer.deleteFeature(right_feat.id())
                    self.working_layer.addFeature(new_feat)
            else:
                print("Skipping " + str(left_feat[self.layer_identifier]) + ", " + str(right_feat[self.layer_identifier]))
            saved = self.working_layer.commitChanges()
            if not saved:
                print("Error saving on compare {left}, {right}".format(left=left_feat[self.layer_identifier], right=right_feat[self.layer_identifier]))
                print(self.working_layer.commitErrors())

        print("Ending handle overlaps. Found " + str(self.overlap_count_this_iteration))
        print("Ending with {count} features".format(count = str(self.working_layer.featureCount())))

    # utility methods
    def lessThanIDFilterString(self):
        # eg Table < Joined_TableID; prevents duplicate/permute records
        return "{identifier} < {join_prefix}{identifier}".format(identifier = self.layer_identifier, join_prefix = JOIN_PREFIX)

    def neqIDFilterString(self):
        # eg TableID != Joined_TableID; prevents within from finding equalities
        return "{identifier} != {join_prefix}{identifier}".format(identifier = self.layer_identifier, join_prefix = JOIN_PREFIX)

    def getLayerIdentifier(self, joined = False):
        if not joined:
            return self.layer_identifier
        else:
            return "{jp}{li}".format(jp=JOIN_PREFIX, li=self.layer_identifier)

    def compareFeatures(self, left_feat, right_feat = None):
        # expected to return true if the left side (or the unjoined side) loses
        if right_feat == None:
            return self.compareFeaturesViaJoinedLayer(left_feat)
        else:
            return self.compareFeaturesViaSeparateLayers(left_feat,right_feat)

    def removeFromCandidateLayer(self, feature_id):
        print("Deleting " + str(feature_id))
        for featToDelete in self.working_layer.getFeatures("{identifier} = {id}".format(identifier = self.layer_identifier, id=feature_id)):
            self.working_layer.deleteFeature(featToDelete.id())

    def addFeatureToIntersectLayer(self, feature_id):
        ## todo: I think this method is unused?
        for featToAppend in self.working_layer.getFeatures("{identifier} = {id}".format(identifier = self.layer_identifier, id=feature_id)):
            self.intersect_layer.addFeature(featToAppend)

if __name__ == '__main__':
    parseArguments()
    
    qgs = QgsApplication([], False, "")
    qgs.initQgis()
        
    Processing.initialize()
    #QgsApplication.processingRegistry().addProvider(QgsNativeAlgorithms())

    # must set processing framework to skip invalid geometries as it defaults to halt-and-catch-fire
    PROCESSING_CONTEXT = dataobjects.createContext()
    PROCESSING_CONTEXT.setInvalidGeometryCheck(QgsFeatureRequest.GeometrySkipInvalid)

    # Set the decision functions for delineations
    def compareDelineationsViaJoinedLayer(feat):
        return feat["TCEffect"] <= feat["{jp}TCEffect".format(jp=JOIN_PREFIX)]
    def compareDelineationsViaSeparateLayers(left_feat, right_feat):
        return left_feat["TCEffect"] <= right_feat["TCEffect"]

    # Set the decision functions for OVTAs
    def compareAssessmentAreasViaJoinedLayer(feat):
        return feat["AssessDate"] <= feat["{jp}AssessDate".format(jp=JOIN_PREFIX)]
    def compareAssessmentAreasViaSeparateLayers(left_feat, right_feat):
        return left_feat["AssessDate"] <= right_feat["AssessDate"]
    
    #Do note that the views here have all input filters built into them
    delineation_layer = fetchLayerFromFileSystem(TGU_INPUT_PATH, "DelineationLayer")
    delineation_layer = bufferZero(delineation_layer, 'bufferdelineations', PROCESSING_CONTEXT)
    print("Flattening Delineations...\n")
    flatten_delineations = Flatten(delineation_layer, "DelinID", compareDelineationsViaJoinedLayer, compareDelineationsViaSeparateLayers, "TCEffect")
    flatten_delineations.run()
    print("\n\n")

    ovta_layer = fetchLayerFromFileSystem(OVTA_INPUT_PATH, "OVTALayer")
    ovta_layer = bufferZero(ovta_layer, 'bufferovta', PROCESSING_CONTEXT)
    print("Flattening OVTAs...\n")
    flatten_ovtas = Flatten(ovta_layer, "OVTAID", compareAssessmentAreasViaJoinedLayer, compareAssessmentAreasViaSeparateLayers, "AssessDate")
    flatten_ovtas.run()
            
    print("Union OVTA with Delineation\n")
    delineation_flattened_layer_path = OUTPUT_FOLDER_AND_FILE_PREFIX + 'delineation_flattened_layer.geojson'
    ovta_flattened_layer_path = OUTPUT_FOLDER_AND_FILE_PREFIX + 'ovta_flattened_layer.geojson'
    ovta_delineation_layer_path = OUTPUT_FOLDER_AND_FILE_PREFIX + 'ovta_delineation_layer.geojson'
    ovta_delineation_layer = unionAndFix(flatten_ovtas.working_layer, flatten_delineations.working_layer, ovta_flattened_layer_path, delineation_flattened_layer_path, ovta_delineation_layer_path, PROCESSING_CONTEXT)

    wqmp_layer = fetchLayerFromFileSystem(WQMP_INPUT_PATH, "WQMPLayer")
    wqmp_layer = bufferZero(wqmp_layer, 'bufferwqmps', PROCESSING_CONTEXT)
    print("Flattening WQMPs...\n")
    flatten_wqmps = Flatten(wqmp_layer, "WQMPID", compareDelineationsViaJoinedLayer, compareDelineationsViaSeparateLayers, "TCEffect")
    flatten_wqmps.run()

    print("Union OVTA-Delineation with WQMP\n")
    wqmp_flattened_layer_path = OUTPUT_FOLDER_AND_FILE_PREFIX + 'wqmp_flattened_layer.geojson'
    ovta_delineation_layer_unionedandfixed_path = OUTPUT_FOLDER_AND_FILE_PREFIX + 'ovta_delineation_layer_unionedandfixed.geojson'
    odw_layer_path = OUTPUT_FOLDER_AND_FILE_PREFIX + 'odw_layer.geojson'
    odw_layer = unionAndFix(ovta_delineation_layer_path, flatten_wqmps.working_layer, ovta_delineation_layer_unionedandfixed_path, wqmp_flattened_layer_path, odw_layer_path, PROCESSING_CONTEXT)

    semifinalOutputPath = OUTPUT_FOLDER_AND_FILE_PREFIX + 'semifinal.geojson'
    print("Union Land Use Block layer with Delineation-OVTA Layer. Will write to: " + semifinalOutputPath)

    # The union will include false TGUs, where there is no land use block ID. The GDAL query will remove those.
    land_use_block_layer = fetchLayerFromFileSystem(LAND_USE_BLOCK_INPUT_PATH, "LUBLayer")
    land_use_block_layer_unionandfixed_path = OUTPUT_FOLDER_AND_FILE_PREFIX + 'land_use_block_layer_unionedandfixed.geojson'
    odw_layer_unionandfixed_path = OUTPUT_FOLDER_AND_FILE_PREFIX + 'odw_layer_unionedandfixed.geojson'
    unionAndFix(land_use_block_layer, odw_layer_path, land_use_block_layer_unionandfixed_path, odw_layer_unionandfixed_path, semifinalOutputPath, PROCESSING_CONTEXT)

    # we are getting line strings back from the union. let's try and remove the bad geometries
    finalOutputPath = OUTPUT_FOLDER_AND_FILE_PREFIX + '.geojson'
    multipartToSinglePart(semifinalOutputPath, None, finalOutputPath, PROCESSING_CONTEXT )

    tgu_layer = fetchLayerFromFileSystem(finalOutputPath, "TGUsNoLines")
    print("Starting removing line strings and bad geometries.")
    print("Starting with {count} features".format(count = str(tgu_layer.featureCount())))

    tgu_layer.startEditing()

    for feat in tgu_layer.getFeatures():
        if feat.geometry().type() == 1: # line strings
            print("Deleting " + str(feat.id()) + " with geometry type " + QgsWkbTypes.geometryDisplayString(feat.geometry().type()))
            tgu_layer.deleteFeature(feat.id())
        elif feat.geometry().area() < 1:
            print("Deleting " + str(feat.id()) + " since it has area < 1")
            tgu_layer.deleteFeature(feat.id())
        elif feat["LUBID"] is None or feat["SJID"] is None:
            print("Deleting " + str(feat.id()) + " since it has no LUBID or SJID")
            tgu_layer.deleteFeature(feat.id())


    tgu_layer.commitChanges()
    print("Ending with {count} features".format(count = str(tgu_layer.featureCount())))
    print("Ending removing line strings and bad geometries.")
    
    print("Succeeded!")