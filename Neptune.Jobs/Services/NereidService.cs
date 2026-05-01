using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neptune.Common;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services;
using Neptune.EFModels.Entities;
using Neptune.EFModels.Nereid;

namespace Neptune.Jobs.Services;

public class NereidService : BaseAPIService<NereidService>
{
    public NereidService(HttpClient httpClient, ILogger<NereidService> logger) : base(httpClient, logger, "Nereid Service")
    {
    }

    public async Task<object> HealthCheck()
    {
        return await GetJsonImpl<object>("/", GeoJsonSerializer.DefaultSerializerOptions);

    }

    public async Task<object> ConfigCheck()
    {
        return await GetJsonImpl<object>("config", GeoJsonSerializer.DefaultSerializerOptions);

    }

    public async Task<NereidResult<TResp>> RunJobAtNereid<TReq, TResp>(TReq nereidRequestObject,
        string nereidRequestUrl, List<Node> graphNodes, NeptuneDbContext? dbContext)
    {
        var serializedRequest = GeoJsonSerializer.Serialize(nereidRequestObject);
        var httpResponseMessage = await PostJsonImpl(nereidRequestUrl, nereidRequestObject, GeoJsonSerializer.DefaultSerializerOptions);
        var postResultContentAsStringResult = await httpResponseMessage.Content.ReadAsStringAsync();

        var nereidResultResponse = GeoJsonSerializer.Deserialize<NereidResult<TResp>>(postResultContentAsStringResult);
        if (nereidResultResponse.Detail != null)
        {
            throw new Exception(nereidResultResponse.Detail.ToString());
        }

        var resultRoute = nereidResultResponse.ResultRoute;
        var executing = true;

        while (executing)
        {
            Logger.LogInformation(resultRoute);
            nereidResultResponse = await HttpClient.GetFromJsonAsync<NereidResult<TResp>>($"{resultRoute}");

            if (nereidResultResponse.Detail != null)
            {
                throw new Exception(nereidResultResponse.Detail.ToString());
            }

            if (nereidResultResponse.Status != NereidJobStatus.STARTED && nereidResultResponse.Status != NereidJobStatus.PENDING)
            {
                executing = false;
            }
            else
            {
                Thread.Sleep(1000);
            }
        }

        if (dbContext != null)
        {
            var serializedResponse = GeoJsonSerializer.Serialize(nereidResultResponse);
            var nereidLog = new NereidLog()
            {
                RequestDate = DateTime.UtcNow,
                NereidRequest = serializedRequest,
                NereidResponse = serializedResponse
            };
            await dbContext.AddAsync(nereidLog);
            await dbContext.SaveChangesAsync();

            var treatmentBMPIDs = graphNodes.Where(x => x.TreatmentBMPID.HasValue).Select(x => x.TreatmentBMPID.Value).ToList();
            if (treatmentBMPIDs.Any())
            {
                await dbContext.TreatmentBMPs.Where(x => treatmentBMPIDs.Contains(x.TreatmentBMPID)).ExecuteUpdateAsync(
                    x => x
                        .SetProperty(y => y.LastNereidLogID, nereidLog.NereidLogID)
                );
            }
            var waterQualityManagementPlanIDs = graphNodes.Where(x => x.WaterQualityManagementPlan != null).Select(x => x.WaterQualityManagementPlan.WaterQualityManagementPlanID).ToList();
            if (waterQualityManagementPlanIDs.Any())
            {
                await dbContext.WaterQualityManagementPlans.Where(x => waterQualityManagementPlanIDs.Contains(x.WaterQualityManagementPlanID)).ExecuteUpdateAsync(
                    x => x
                        .SetProperty(y => y.LastNereidLogID, nereidLog.NereidLogID)
                );
            }

        }

        return nereidResultResponse;
    }

    public async Task<NetworkSolveResult> DeltaSolve(NeptuneDbContext dbContext, List<DirtyModelNode> dirtyModelNodes, bool isBaselineCondition)
    {
        var graph = BuildTotalNetworkGraph(dbContext);

        var dirtyTreatmentBMPIDs = dirtyModelNodes.Where(x => x.TreatmentBMPID != null).Select(y => y.TreatmentBMPID).ToList();
        var dirtyDelineationIDs = dirtyModelNodes.Where(x => x.DelineationID != null).Select(y => y.DelineationID).ToList();
        var dirtyRegionalSubbasinIDs = dirtyModelNodes.Where(x => x.RegionalSubbasinID != null).Select(y => y.RegionalSubbasinID).ToList();
        var dirtyWaterQualityManagementPlanIDs = dirtyModelNodes.Where(x => x.WaterQualityManagementPlanID != null).Select(y => y.WaterQualityManagementPlanID).ToList();

        var dirtyGraphNodes = graph.Nodes.Where(x => dirtyTreatmentBMPIDs.Contains(x.TreatmentBMPID) ||
                                                     dirtyDelineationIDs.Contains(x.Delineation?.DelineationID) ||
                                                     dirtyRegionalSubbasinIDs
                                                         .Contains(x.RegionalSubbasinID) ||
                                                     dirtyWaterQualityManagementPlanIDs
                                                         .Contains(x.WaterQualityManagementPlan?
                                                             .WaterQualityManagementPlanID)).ToList();

        const string subgraphUrl = "api/v1/network/subgraph";

        var subgraphRequestObject = new NereidSubgraphRequestObject(graph, dirtyGraphNodes);

        var subgraphResult = await RunJobAtNereid<NereidSubgraphRequestObject, SubgraphResult>(subgraphRequestObject,
            subgraphUrl, subgraphRequestObject.Nodes, dbContext);
        List<Node> nodesForSubgraph;
        try
        {
            nodesForSubgraph = subgraphResult.Data.SubgraphNodes.SelectMany(x => x.Nodes).Distinct().ToList();
        }
        catch (Exception e)
        {
            throw new NereidException<NereidSubgraphRequestObject, SubgraphResult>($"Exception thrown accessing result of subgraph call. Status was {subgraphResult.Status}.", e);
        }

        var deltaSubgraph = MakeSubgraphFromParentGraphAndNodes(graph, nodesForSubgraph);

        var networkSolveResult = await NetworkSolveImpl(deltaSubgraph, dbContext, true, isBaselineCondition, null, null, null);

        var scenarioNereidResults =
            dbContext.NereidResults.Where(x => x.IsBaselineCondition == isBaselineCondition).ToList();

        scenarioNereidResults.MergeUpsert(networkSolveResult.NereidResults.ToList(), dbContext.NereidResults, (old, novel) =>
            old.NodeID == novel.NodeID
        , (old, novel) =>
        {
            old.FullResponse = novel.FullResponse;
            old.LastUpdate = novel.LastUpdate;
        });

        await dbContext.SaveChangesAsync();

        return networkSolveResult;
    }

    private async Task<NetworkSolveResult> NetworkSolveImpl(Graph graph, NeptuneDbContext dbContext, bool sendPreviousResults, bool isBaselineCondition, int? projectID, List<int>? projectRegionalSubbasinIDs, List<int>? projectDistributedDelineationIDs)
    {
        const string solutionSequenceUrl = "api/v1/network/solution_sequence?min_branch_size=12";

        var allLoadingInputs = projectID != null ? dbContext.vNereidProjectLoadingInputs.Where(x => x.ProjectID == projectID).ToList().Select(x =>
            new vNereidLoadingInput()
            {
                Area = x.Area,
                BaselineImperviousAcres = x.BaselineImperviousAcres,
                BaselineLandUseCode = x.BaselineLandUseCode,
                DelineationID = x.DelineationID,
                DelineationIsVerified = x.DelineationIsVerified,
                HydrologicSoilGroup = x.HydrologicSoilGroup,
                ImperviousAcres = x.ImperviousAcres,
                LandUseCode = x.LandUseCode,
                ModelBasinKey = x.ModelBasinKey,
                OCSurveyCatchmentID = x.OCSurveyCatchmentID,
                RegionalSubbasinID = x.RegionalSubbasinID,
                SlopePercentage = x.SlopePercentage,
                RelationallyAssociatedModelingApproach = x.RelationallyAssociatedModelingApproach,
                SpatiallyAssociatedModelingApproach = x.SpatiallyAssociatedModelingApproach,
                WaterQualityManagementPlanID = x.WaterQualityManagementPlanID
            }).ToList() : dbContext.vNereidLoadingInputs.ToList();
        var allModelingBMPs = TreatmentBMPs.ListModelingTreatmentBMPs(dbContext, projectID, projectRegionalSubbasinIDs).ToList();
        var allWQMPNodes =
            GetWaterQualityManagementPlanNodes(dbContext, projectID, projectRegionalSubbasinIDs).ToList();
        //This will get taken care of inside of SolveSubgraph based on the WQMP nodes above, so no need to filter it here
        var allModelingQuickBMPs = dbContext.QuickBMPs.Include(x => x.TreatmentBMPType).Include(x => x.WaterQualityManagementPlan).Where(x =>
                x.PercentOfSiteTreated != null && x.PercentCaptured != null && x.PercentRetained != null &&
                x.TreatmentBMPType.IsAnalyzedInModelingModule)
            .ToList();
        var delineations = vTreatmentBMPUpstreams.ListWithDelineationAsDictionary(dbContext);

        var solutionSequenceResult =
            await RunJobAtNereid<SolutionSequenceRequest, SolutionSequenceResult>(
                new SolutionSequenceRequest(graph), solutionSequenceUrl, graph.Nodes, dbContext);

        // for the delta run, associate each node with its previous results
        if (sendPreviousResults)
        {
            var previousModelResults = dbContext.NereidResults.ToList();
            foreach (var node in graph.Nodes)
            {
                var previousNodeResults = previousModelResults.SingleOrDefault(x =>
                    node.ID == x.NodeID && x.IsBaselineCondition == isBaselineCondition
                    )?.FullResponse;

                if (previousNodeResults != null)
                {
                    var previousResults = GeoJsonSerializer.Deserialize<JsonObject>(previousNodeResults);
                    //In terms of running a delta solve, this means we likely already have results for a large number of nodes. So, the current results start as the previous results.
                    //This is important for ensuring we don't overwrite good values down the line if they aren't actually being solved for.
                    node.Results = previousResults;
                    node.PreviousResults = previousResults;
                    node.PreviousResults["node_errors"] = "";
                    node.PreviousResults["node_warnings"] = "";
                }
            }
        }

        var modelBasins = dbContext.ModelBasins.ToDictionary(x => x.ModelBasinID, x => x.ModelBasinKey);
        var precipitationZones = dbContext.PrecipitationZones.ToDictionary(x => x.PrecipitationZoneID, x => x.DesignStormwaterDepthInInches);
        var treatmentBMPModelingAttributes = vTreatmentBMPModelingAttributes.ListAsDictionary(dbContext);
        var missingNodeIDs = new List<string>();
        foreach (var parallel in solutionSequenceResult.Data.SolutionSequence.Parallel)
        {
            foreach (var series in parallel.Series)
            {
                var seriesNodes = series.Nodes;
                var subgraph = MakeSubgraphFromParentGraphAndNodes(graph, seriesNodes);

                var notFoundNodes = await SolveSubgraph(subgraph, allLoadingInputs, allModelingBMPs, allWQMPNodes,
                    allModelingQuickBMPs, isBaselineCondition, modelBasins, precipitationZones, delineations, treatmentBMPModelingAttributes, projectDistributedDelineationIDs, dbContext);
                missingNodeIDs.AddRange(notFoundNodes);
            }
        }

        var nereidResults = graph.Nodes.Where(x => x.Results != null).Select(x => new NereidResult
        {
            FullResponse = x.Results.ToString(),
            IsBaselineCondition = isBaselineCondition,
            TreatmentBMPID = x.TreatmentBMPID,
            DelineationID = x.Delineation?.DelineationID,
            NodeID = x.ID,
            RegionalSubbasinID = x.RegionalSubbasinID,
            WaterQualityManagementPlanID = x.WaterQualityManagementPlan?.WaterQualityManagementPlanID,
            LastUpdate = DateTime.UtcNow
        }).ToList();

        return new NetworkSolveResult(nereidResults, graph, missingNodeIDs);
    }

    public async Task<NetworkSolveResult> TotalNetworkSolve(NeptuneDbContext dbContext, bool isBaselineCondition)
    {
        var graph = BuildTotalNetworkGraph(dbContext);
        var networkSolveResult = await NetworkSolveImpl(graph, dbContext, false, isBaselineCondition, null, null, null);

        dbContext.NereidResults.AddRange(networkSolveResult.NereidResults);
        await dbContext.SaveChangesAsync();

        return networkSolveResult;
    }

    public async Task<NetworkSolveResult> ProjectNetworkSolve(NeptuneDbContext dbContext, bool isBaselineCondition, int projectID, List<int> projectRSBIDs, List<int> projectDistributedDelineationIDs)
    {
        var graph = BuildProjectNetworkGraph(dbContext, projectID, projectRSBIDs);

        var networkSolveResult = await NetworkSolveImpl(graph, dbContext, false, isBaselineCondition, projectID, projectRSBIDs, projectDistributedDelineationIDs);

        var projectIDSqlParam = new SqlParameter("@projectID", projectID);
        await dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pDeleteProjectNereidResults @projectID", projectIDSqlParam);

        dbContext.ProjectNereidResults.AddRange(networkSolveResult.NereidResults.Select(x =>
            new ProjectNereidResult
            {
                ProjectID = projectID,
                IsBaselineCondition = x.IsBaselineCondition,
                FullResponse = x.FullResponse,
                LastUpdate = x.LastUpdate,
                NodeID = x.NodeID,
                RegionalSubbasinID = x.RegionalSubbasinID,
                TreatmentBMPID = x.TreatmentBMPID,
                WaterQualityManagementPlanID = x.WaterQualityManagementPlanID,
                DelineationID = x.DelineationID
            }
            ).ToList());
        await dbContext.SaveChangesAsync();

        // we are intentionally caching the score here to the project table for speed purposes
        await dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pProjectGrantScoreUpdate @projectID", projectIDSqlParam);

        return networkSolveResult;
    }



    public async Task<List<string>> SolveSubgraph(Graph subgraph,
        List<vNereidLoadingInput> allLoadingInputs, List<TreatmentBMP> allModelingBMPs,
        List<WaterQualityManagementPlanNode> allWaterqualityManagementPlanNodes,
        List<QuickBMP> allModelingQuickBMPs, bool isBaselineCondition, Dictionary<int, int> modelBasins,
        Dictionary<int, double> precipitationZones, Dictionary<int, Delineation?> delineations,
        Dictionary<int, vTreatmentBMPModelingAttribute> treatmentBMPModelingAttributes,
        List<int>? projectDelineationIDs, NeptuneDbContext? dbContext)
    {
        var notFoundNodes = new List<string>();

        // Now I need to get the land_surface, treatment_facility, and treatment_site tables for this request.
        // these are going to look very much like the various calls made throughout the testing methods, but filtered
        // to the subgraph. Fortunately, we've added metadata to the nodes to help us do the filtration

        var delineationToIncludeIDs = subgraph.Nodes.Where(x => x.Delineation != null).Select(x => x.Delineation.DelineationID)
            .Distinct().ToList();
        var regionalSubbasinToIncludeIDs = subgraph.Nodes.Where(x => x.RegionalSubbasinID != null)
            .Select(x => x.RegionalSubbasinID).Distinct().ToList();
        var waterQualityManagementPlanToIncludeIDs = subgraph.Nodes.Where(x => x.WaterQualityManagementPlan != null)
            .Select(x => x.WaterQualityManagementPlan.WaterQualityManagementPlanID).Distinct().ToList();
        var treatmentBMPToIncludeIDs = subgraph.Nodes.Where(x => x.TreatmentBMPID != null)
            .Select(x => x.TreatmentBMPID.Value).Distinct().ToList();

        var landSurfaces = allLoadingInputs.Where(x =>
            delineationToIncludeIDs.Contains(x.DelineationID.GetValueOrDefault()) ||
            regionalSubbasinToIncludeIDs.Contains(x.RegionalSubbasinID) ||
            waterQualityManagementPlanToIncludeIDs.Contains(x.WaterQualityManagementPlanID.GetValueOrDefault())
        ).ToList().Select(x => new LandSurface(x, isBaselineCondition, projectDelineationIDs)).ToList();

        var treatmentFacilities = allModelingBMPs
            .Where(x => treatmentBMPToIncludeIDs.Contains(x.TreatmentBMPID) &&
                        // Don't create TreatmentFacilities for BMPs belonging to a Simple WQMP
                        x.WaterQualityManagementPlan?.WaterQualityManagementPlanModelingApproachID !=  (int) WaterQualityManagementPlanModelingApproachEnum.Simplified)
            .Select(x => x.ToTreatmentFacility(delineations, isBaselineCondition, modelBasins, precipitationZones, treatmentBMPModelingAttributes.TryGetValue(x.TreatmentBMPID, out var attribute) ? attribute : null)).ToList();

        var filteredQuickBMPs = allModelingQuickBMPs
            .Where(x => waterQualityManagementPlanToIncludeIDs.Contains(x.WaterQualityManagementPlanID) &&
                        // Don't create TreatmentSites for QuickBMPs belonging to a Detailed WQMP
                        x.WaterQualityManagementPlan?.WaterQualityManagementPlanModelingApproachID != WaterQualityManagementPlanModelingApproach.Detailed.WaterQualityManagementPlanModelingApproachID).ToList();
        var filteredWQMPNodes = allWaterqualityManagementPlanNodes.Where(y =>
                waterQualityManagementPlanToIncludeIDs.Contains(y.WaterQualityManagementPlanID) &&
                regionalSubbasinToIncludeIDs.Contains(y.RegionalSubbasinID) // ignore parts that live in RSBs outside our solve area.
        ).ToList();

        var treatmentSites = filteredQuickBMPs
            .Join(
                filteredWQMPNodes, x => x.WaterQualityManagementPlanID,
                x => x.WaterQualityManagementPlanID, (bmp, node) => new { bmp, node })
            .Select(x =>
                new TreatmentSite
                {
                    NodeID = NereidUtilities.WaterQualityManagementPlanTreatmentNodeID(x.node.WaterQualityManagementPlanID,
                        x.node.OCSurveyCatchmentID),
                    AreaPercentage = x.bmp.PercentOfSiteTreated,
                    CapturedPercentage = x.bmp.PercentCaptured ?? 0,
                    RetainedPercentage = x.bmp.PercentRetained ?? 0,
                    // treat wqmps built after 2003 as if they don't exist.
                    FacilityType = (isBaselineCondition && x.node.DateOfConstruction.HasValue && x.node.DateOfConstruction.Value.Year > Constants.NEREID_BASELINE_CUTOFF_YEAR) ? "NoTreatment" : x.bmp.TreatmentBMPType.TreatmentBMPModelingType.TreatmentBMPModelingTypeName,
                    EliminateAllDryWeatherFlowOverride = x.bmp.DryWeatherFlowOverrideID == DryWeatherFlowOverride.Yes.DryWeatherFlowOverrideID

                }).ToList();

        //ValidateForTesting(subgraph, landSurfaces, treatmentFacilities, treatmentSites);

        const string solveUrl = "api/v1/watershed/solve?state=ca&region=oc";

        // get the list of leaf nodes for this subgraph
        var targetNodeIDs = subgraph.Edges.Select(x => x.TargetID);
        // As all men know in this kingdom by the sea, a leaf of a digraph is a node that's not the target of an edge
        var leafNodes = subgraph.Nodes.Where(x => !targetNodeIDs.Contains(x.ID));

        var solutionRequestObject = new SolutionRequestObject()
        {
            Graph = subgraph,
            LandSurfaces = landSurfaces,
            TreatmentFacilities = treatmentFacilities,
            TreatmentSites = treatmentSites,
            PreviousResults = leafNodes.Where(x => x.PreviousResults != null).Select(x => x.PreviousResults).ToList()
        };
        NereidResult<SolutionResponseObject> results = null;
        try
        {
            results = await RunJobAtNereid<SolutionRequestObject, SolutionResponseObject>(solutionRequestObject, solveUrl, solutionRequestObject.Graph.Nodes, dbContext);
        }
        catch (Exception e)
        {
            throw new NereidException<SolutionRequestObject, SolutionResponseObject>(e.Message, e)
            {
                Request = solutionRequestObject,
                Response = results?.Data
            };
        }

        if (results?.Data.Errors != null && results.Data.Errors.Count > 0 && (results.Data.Results == null || results.Data.Results.Count == 0))
        {
            throw new NereidException<SolutionRequestObject, SolutionResponseObject>
            { Request = solutionRequestObject, Response = results.Data };
        }

        // literally this can't be null...
        // ReSharper disable once PossibleNullReferenceException
        var previousResultsKeys = results.Data.PreviousResultsKeys;
        foreach (var dataLeafResult in results.Data.Results)
        {
            var node = subgraph.Nodes.SingleOrDefault(x => x.ID == dataLeafResult["node_id"].ToString());
            if (node == null)
            {
                // this is an edge case that should only happen if an RSB in the SOC area has
                // its downstream catchment outside the SOC area for some reason.
                notFoundNodes.Add(dataLeafResult["node_id"].ToString());
            }
            else
            {
                node.Results = dataLeafResult;

                // track the smaller subset of results that need to be sent for subsequent calls
                var previousResults = new JsonObject();
                foreach (var key in previousResultsKeys)
                {
                    var value = JsonSerializer.SerializeToNode(dataLeafResult[key], GeoJsonSerializer.DefaultSerializerOptions);
                    previousResults.Add(key, value);
                }

                node.PreviousResults = previousResults;
            }
        }

        foreach (var dataLeafResult in results.Data.LeafResults)
        {
            try
            {
                var node = subgraph.Nodes.SingleOrDefault(x => x.ID == dataLeafResult["node_id"].ToString());
                if (node == null)
                {
                    notFoundNodes.Add(dataLeafResult["node_id"].ToString());
                }
                else
                {
                    // don't store the leaf results if already data at this node--most of the time these nodes are read-only
                    if (node.Results == null)
                    {
                        node.Results = dataLeafResult;
                    }
                }
            }
            catch (InvalidOperationException ioe)
            {
                throw new DuplicateNodeException(dataLeafResult["node_id"].ToString(), ioe);
            }
        }

        return notFoundNodes;
    }

    private static Graph MakeSubgraphFromParentGraphAndNodes(Graph graph, List<Node> nodes)
    {
        var subgraphNodeIDs = nodes.Select(x => x.ID).ToList();

        // create the subgraph that has these nodes as its nodes and the appropriate edges
        // appropriate edges = where target in nodes?
        var subgraphNodes = graph.Nodes.Where(x => subgraphNodeIDs.Contains(x.ID)).ToList();
        var subgraphEdges = graph.Edges.Where(x =>
            subgraphNodeIDs.Contains(x.TargetID) && subgraphNodeIDs.Contains(x.SourceID)).ToList();

        var subgraph = new Graph(true, subgraphNodes, subgraphEdges);
        return subgraph;
    }


    public Graph BuildTotalNetworkGraph(NeptuneDbContext dbContext)
    {
        var nodes = new List<Node>();
        var edges = new List<Edge>();

        MakeRSBNodesAndEdges(dbContext.RegionalSubbasins.Where(x => x.IsInModelBasin == true).ToList(), out var rsbEdges, out var rsbNodes);
        nodes.AddRange(rsbNodes);
        edges.AddRange(rsbEdges);

        MakeDistributedBMPNodesAndEdges(dbContext, out var distributedBMPEdges, out var distributedBMPNodes);
        nodes.AddRange(distributedBMPNodes);
        edges.AddRange(distributedBMPEdges);

        MakeDistributedDelineationNodesAndEdges(dbContext, out var delineationEdges, out var delineationNodes);
        nodes.AddRange(delineationNodes);
        edges.AddRange(delineationEdges);

        MakeUpstreamBMPNodesAndEdges(dbContext, out var colocationEdges, out var colocationNodes);
        nodes.AddRange(colocationNodes);
        edges.AddRange(colocationEdges);

        MakeWQMPNodesAndEdges(dbContext, out var wqmpEdges, out var wqmpNodes);
        nodes.AddRange(wqmpNodes);
        edges.AddRange(wqmpEdges);

        MakeCentralizedBMPNodesAndEdges(dbContext, out var centralizedBMPEdges, out var centralizedBMPNodes, edges);
        nodes.AddRange(centralizedBMPNodes);
        edges.AddRange(centralizedBMPEdges);

        var graph = new Graph(true, nodes, edges);
        return graph;
    }

    public Graph BuildProjectNetworkGraph(NeptuneDbContext dbContext, int projectID, List<int> projectRSBIDs)
    {
        var nodes = new List<Node>();
        var edges = new List<Edge>();

        MakeRSBNodesAndEdges(dbContext.RegionalSubbasins.Where(x => projectRSBIDs.Contains(x.RegionalSubbasinID)).ToList(), out var rsbEdges, out var rsbNodes);
        nodes.AddRange(rsbNodes);
        edges.AddRange(rsbEdges);

        MakeDistributedBMPNodesAndEdges(dbContext, out var distributedBMPEdges, out var distributedBMPNodes, projectID, projectRSBIDs);
        nodes.AddRange(distributedBMPNodes);
        edges.AddRange(distributedBMPEdges);

        MakeDistributedDelineationNodesAndEdges(dbContext, out var delineationEdges, out var delineationNodes, projectID, projectRSBIDs);
        nodes.AddRange(delineationNodes);
        edges.AddRange(delineationEdges);

        MakeUpstreamBMPNodesAndEdges(dbContext, out var colocationEdges, out var colocationNodes, projectRSBIDs);
        nodes.AddRange(colocationNodes);
        edges.AddRange(colocationEdges);

        MakeWQMPNodesAndEdges(dbContext, out var wqmpEdges, out var wqmpNodes, projectID, projectRSBIDs);
        nodes.AddRange(wqmpNodes);
        edges.AddRange(wqmpEdges);

        MakeCentralizedBMPNodesAndEdges(dbContext, out var centralizedBMPEdges, out var centralizedBMPNodes, edges, projectID, projectRSBIDs);
        nodes.AddRange(centralizedBMPNodes);
        edges.AddRange(centralizedBMPEdges);

        var graph = new Graph(true, nodes, edges);
        return graph;
    }

    private static void MakeRSBNodesAndEdges(List<RegionalSubbasin> regionalSubbasinsInCoverage, out List<Edge> rsbEdges, out List<Node> rsbNodes)
    {
        rsbNodes = regionalSubbasinsInCoverage
            .Select(x => new Node { ID = NereidUtilities.RegionalSubbasinNodeID(x.OCSurveyCatchmentID), RegionalSubbasinID = x.RegionalSubbasinID }).ToList();

        rsbEdges = regionalSubbasinsInCoverage
            .Where(x => x.OCSurveyDownstreamCatchmentID != null).Select(x =>
                new Edge()
                {
                    SourceID = NereidUtilities.RegionalSubbasinNodeID(x.OCSurveyCatchmentID),
                    TargetID = NereidUtilities.RegionalSubbasinNodeID(x.OCSurveyDownstreamCatchmentID.Value)
                }).ToList();
    }

    private static void MakeDistributedBMPNodesAndEdges(NeptuneDbContext dbContext, out List<Edge> distributedBMPEdges, out List<Node> distributedBMPNodes, int? projectID = null, List<int> projectRegionalSubbasinIDs = null)
    {
        var vNereidTreatmentBMPRegionalSubbasins = dbContext.vNereidTreatmentBMPRegionalSubbasins.ToList();
        if (projectRegionalSubbasinIDs != null && projectRegionalSubbasinIDs.Any())
        {
            vNereidTreatmentBMPRegionalSubbasins = vNereidTreatmentBMPRegionalSubbasins.Where(x => projectRegionalSubbasinIDs.Contains(x.RegionalSubbasinID)).ToList();
        }

        distributedBMPNodes = vNereidTreatmentBMPRegionalSubbasins
            .Select(x => new Node() { ID = NereidUtilities.TreatmentBMPNodeID(x.TreatmentBMPID), TreatmentBMPID = x.TreatmentBMPID }).ToList();

        distributedBMPEdges = vNereidTreatmentBMPRegionalSubbasins.Select(x => new Edge()
        {
            SourceID = NereidUtilities.TreatmentBMPNodeID(x.TreatmentBMPID),
            TargetID = NereidUtilities.RegionalSubbasinNodeID(x.OCSurveyCatchmentID)
        }).ToList();

        if (projectID != null)
        {
            var vNereidProjectTreatmentBMPRegionalSubbasins = dbContext.vNereidProjectTreatmentBMPRegionalSubbasins.Where(x => x.ProjectID == projectID).ToList();
            distributedBMPNodes.AddRange(vNereidProjectTreatmentBMPRegionalSubbasins.Select(x => new Node() { ID = NereidUtilities.TreatmentBMPNodeID(x.TreatmentBMPID), TreatmentBMPID = x.TreatmentBMPID }).ToList());
            distributedBMPEdges.AddRange(vNereidProjectTreatmentBMPRegionalSubbasins.Select(x => new Edge()
            {
                SourceID = NereidUtilities.TreatmentBMPNodeID(x.TreatmentBMPID),
                TargetID = NereidUtilities.RegionalSubbasinNodeID(x.OCSurveyCatchmentID)
            }).ToList());
        }
    }

    private static void MakeDistributedDelineationNodesAndEdges(NeptuneDbContext dbContext, out List<Edge> delineationEdges, out List<Node> delineationNodes, int? projectID = null, List<int> projectRegionalSubbasinIDs = null)
    {
        var distributedDelineations = dbContext.Delineations.Include(x => x.TreatmentBMP)
            .Where(x => x.DelineationTypeID == (int) DelineationTypeEnum.Distributed &&
                        // don't include delineations for non-modeled BMPs
                        x.TreatmentBMP.TreatmentBMPType.IsAnalyzedInModelingModule &&
                        x.TreatmentBMP.RegionalSubbasinID != null &&
                        x.TreatmentBMP.ModelBasinID != null).ToList();

        if (projectID != null && projectRegionalSubbasinIDs != null)
        {
            distributedDelineations = distributedDelineations.Where(x =>
                         projectRegionalSubbasinIDs.Contains(x.TreatmentBMP.RegionalSubbasinID.Value) &&
                        ((x.TreatmentBMP.ProjectID == null && x.IsVerified) || x.TreatmentBMP.ProjectID == projectID)).ToList();
        }
        else
        {
            distributedDelineations = distributedDelineations.Where(x =>
                        // don't include the provisionals
                        x.IsVerified &&
                        x.TreatmentBMP.ProjectID == null).ToList();
        }

        delineationNodes = distributedDelineations
            .Select(x => new Node()
            {
                ID = NereidUtilities.DelineationNodeID(x.DelineationID),
                Delineation = x
            }).ToList();

        delineationEdges = distributedDelineations
            .Select(x => new Edge()
            {
                SourceID = NereidUtilities.DelineationNodeID(x.DelineationID),
                TargetID = NereidUtilities.TreatmentBMPNodeID(x.TreatmentBMP.TreatmentBMPID)
            }).ToList();
    }

    private static void MakeUpstreamBMPNodesAndEdges(NeptuneDbContext dbContext, out List<Edge> colocationEdges, out List<Node> colocationNodes, List<int> projectRegionalSubbasinIDs = null)
    {
        // we only need to add the upstream nodes, because any bmp that's not an upstream node has already been added

        var bmpColocations = dbContext.vNereidBMPColocations.ToList();

        if (projectRegionalSubbasinIDs != null)
        {
            bmpColocations = bmpColocations.Where(x => x.DownstreamRSBID != null && x.UpstreamRSBID != null && projectRegionalSubbasinIDs.Contains(x.DownstreamRSBID.Value) && projectRegionalSubbasinIDs.Contains(x.UpstreamRSBID.Value)).ToList();
        }
        colocationNodes = bmpColocations
            .Select(x => new Node()
            {
                ID = NereidUtilities.TreatmentBMPNodeID(x.UpstreamBMPID),
                TreatmentBMPID = x.UpstreamBMPID
            }).ToList();

        colocationEdges = bmpColocations
            .Select(x => new Edge()
            {
                SourceID = NereidUtilities.TreatmentBMPNodeID(x.UpstreamBMPID),
                TargetID = NereidUtilities.TreatmentBMPNodeID(x.DownstreamBMPID)
            }).ToList();
    }

    private void MakeWQMPNodesAndEdges(NeptuneDbContext dbContext, out List<Edge> wqmpEdges, out List<Node> wqmpNodes, int? projectID = null, List<int> projectRegionalSubbasinIDs = null)
    {
        var wqmpRSBPairings = GetWaterQualityManagementPlanNodes(dbContext, projectID, projectRegionalSubbasinIDs).ToList();

        wqmpNodes = wqmpRSBPairings.Select(x => new Node
        {
            ID = NereidUtilities.WaterQualityManagementPlanNodeID(x.WaterQualityManagementPlanID, x.OCSurveyCatchmentID),
            WaterQualityManagementPlan = x,
            RegionalSubbasinID = x.RegionalSubbasinID

        }).ToList();

        // WQMPs get two nodes-- one for the land surface data and one for the treatment data
        var tmntNodes = wqmpRSBPairings.Select(x => new Node
        {
            ID = NereidUtilities.WaterQualityManagementPlanTreatmentNodeID(x.WaterQualityManagementPlanID, x.OCSurveyCatchmentID),
            WaterQualityManagementPlan = x,
            RegionalSubbasinID = x.RegionalSubbasinID
        }).ToList();

        wqmpNodes.AddRange(tmntNodes);

        // the land surface node flows to the treatment node to the rsb node
        wqmpEdges = wqmpRSBPairings.Select(x => new Edge
        {
            SourceID = NereidUtilities.WaterQualityManagementPlanNodeID(x.WaterQualityManagementPlanID, x.OCSurveyCatchmentID),
            TargetID = NereidUtilities.WaterQualityManagementPlanTreatmentNodeID(x.WaterQualityManagementPlanID, x.OCSurveyCatchmentID)
        }).ToList();

        var tmntEdges = wqmpRSBPairings.Select(x => new Edge
        {
            SourceID = NereidUtilities.WaterQualityManagementPlanTreatmentNodeID(x.WaterQualityManagementPlanID, x.OCSurveyCatchmentID),
            TargetID = NereidUtilities.RegionalSubbasinNodeID(x.OCSurveyCatchmentID)
        }).ToList();

        wqmpEdges.AddRange(tmntEdges);
    }

    public List<WaterQualityManagementPlanNode> GetWaterQualityManagementPlanNodes(NeptuneDbContext dbContext, int? projectID = null, List<int> projectRegionalSubbasinIDs = null)
    {
        var activeStatusID = (int)WaterQualityManagementPlanStatusEnum.Active;
        var wqmpns = dbContext.LoadGeneratingUnits.Include(x => x.RegionalSubbasin).Include(x => x.WaterQualityManagementPlan)
            .Where(x => x.WaterQualityManagementPlanID != null && x.RegionalSubbasinID != null
                && x.WaterQualityManagementPlan.WaterQualityManagementPlanStatusID == activeStatusID)
            .Select(x => new WaterQualityManagementPlanNode
            {
                WaterQualityManagementPlanID = x.WaterQualityManagementPlanID.Value,
                RegionalSubbasinID = x.RegionalSubbasinID.Value,
                OCSurveyCatchmentID = x.RegionalSubbasin.OCSurveyCatchmentID,
                DateOfConstruction = x.WaterQualityManagementPlan.DateOfConstruction
            }).Distinct().ToList();

        if (projectID != null && projectRegionalSubbasinIDs != null)
        {
            var ppwqmpns = dbContext.ProjectLoadGeneratingUnits.Include(x => x.RegionalSubbasin).Include(x => x.WaterQualityManagementPlan)
                .Where(x => x.WaterQualityManagementPlanID != null && x.RegionalSubbasinID != null && x.ProjectID == projectID
                    && x.WaterQualityManagementPlan.WaterQualityManagementPlanStatusID == activeStatusID)
                .Select(x => new WaterQualityManagementPlanNode
                {
                    WaterQualityManagementPlanID = x.WaterQualityManagementPlanID.Value,
                    RegionalSubbasinID = x.RegionalSubbasinID.Value,
                    OCSurveyCatchmentID = x.RegionalSubbasin.OCSurveyCatchmentID,
                    DateOfConstruction = x.WaterQualityManagementPlan.DateOfConstruction
                }).Distinct().ToList();
            wqmpns = wqmpns.Where(x => projectRegionalSubbasinIDs.Contains(x.RegionalSubbasinID)).ToList();
            wqmpns.AddRange(ppwqmpns);
            wqmpns = wqmpns.Distinct(new WaterQualityManagementPlanNodeComparer()).ToList();
        }

        return wqmpns;
    }

    private void MakeCentralizedBMPNodesAndEdges(NeptuneDbContext dbContext, out List<Edge> centralizedBMPEdges,
        out List<Node> centralizedBMPNodes, List<Edge> existingEdges, int? projectID = null, List<int> projectRegionalSubbasinIDs = null)
    {
        centralizedBMPNodes = new List<Node>();
        centralizedBMPEdges = new List<Edge>();

        // todo: We're selecting by RowNumber == 1 so the network isn't invalid when there are
        // multiple centralized delineations per one regional subbasin. In future, we'll need
        // to find something better to do about that case than ignore the additionals.

        var centralizedBMPs = dbContext.vNereidRegionalSubbasinCentralizedBMPs.Where(x => x.RowNumber == 1).ToList();

        if (projectID != null && projectRegionalSubbasinIDs != null)
        {
            var projectCentralizedBMPs = dbContext.vNereidProjectRegionalSubbasinCentralizedBMPs.Where(x => x.RowNumber == 1 && x.ProjectID == projectID).ToList().Select(x =>
                new vNereidRegionalSubbasinCentralizedBMP()
                {
                    OCSurveyCatchmentID = x.OCSurveyCatchmentID,
                    RegionalSubbasinID = x.RegionalSubbasinID,
                    RowNumber = x.RowNumber,
                    TreatmentBMPID = x.TreatmentBMPID,
                    //3/16/22 at this point these will always be null, but in the future that may change
                    UpstreamBMPID = x.UpstreamBMPID
                }).ToList();
            //this might be incorrect, but since we're only grabbing 1 per rsb lets prioritize our planned project ones in this case
            centralizedBMPs = centralizedBMPs.Where(x => projectRegionalSubbasinIDs.Contains(x.RegionalSubbasinID) && projectCentralizedBMPs.All(y => y.RegionalSubbasinID != x.RegionalSubbasinID)).ToList();
            centralizedBMPs.AddRange(projectCentralizedBMPs);
        }

        foreach (var rsbCentralizedBMPPairing in centralizedBMPs)
        {
            var newCentralizedBMPNodeID = NereidUtilities.TreatmentBMPNodeID(rsbCentralizedBMPPairing.TreatmentBMPID);
            var existingRSBNodeID = NereidUtilities.RegionalSubbasinNodeID(rsbCentralizedBMPPairing.OCSurveyCatchmentID);

            var edgeFromRegionalSubbasinToDownstreamRegionalSubbasin = existingEdges.SingleOrDefault(x => x.SourceID == existingRSBNodeID);
            var downstreamRegionalSubbasinID = edgeFromRegionalSubbasinToDownstreamRegionalSubbasin?.TargetID;

            var centralizedBMPHasDownstreamBMP =
                dbContext.TreatmentBMPs.Any(x => x.UpstreamBMPID == rsbCentralizedBMPPairing.TreatmentBMPID);

            // If this is null, it means this RSB is at the end of the flow network.
            if (downstreamRegionalSubbasinID != null)
            {
                // Current rsb target equals centralized bmp node id
                edgeFromRegionalSubbasinToDownstreamRegionalSubbasin.TargetID = newCentralizedBMPNodeID;
            }
            // only *create* an edge from the RSB to the Cent if the RSB didn't already have an edge, since in that case
            // this created edge would be identical to the modified one from above
            else
            {
                centralizedBMPEdges.Add(new Edge
                { SourceID = existingRSBNodeID, TargetID = newCentralizedBMPNodeID });
            }

            if (centralizedBMPHasDownstreamBMP)
            {
                // we need to go downstream from this BMP until we hit an edge that flows to this RSB,
                // then we either point that edge at the downstream RSB, or we delete it, depending on if
                // the downstream RSB exists or doesn't respectively.

                // these edges are all guaranteed to exist since we ran MakeUpstreamBMPNodesAndEdges already
                var edgeToDownstreamNode = existingEdges.Single(x => x.SourceID == newCentralizedBMPNodeID);
                while (edgeToDownstreamNode.TargetID != existingRSBNodeID)
                {
                    var currentTargetID = edgeToDownstreamNode.TargetID;
                    edgeToDownstreamNode = existingEdges.SingleOrDefault(x => x.SourceID == currentTargetID);
                    if (edgeToDownstreamNode == null)
                    {
                        Logger.LogInformation($"No existingEdges found with SourceID {currentTargetID}");
                        break;
                    }
                }

                if (edgeToDownstreamNode != null)
                {
                    if (downstreamRegionalSubbasinID != null)
                    {
                        edgeToDownstreamNode.TargetID = downstreamRegionalSubbasinID;
                    }
                    else
                    {
                        existingEdges.Remove(edgeToDownstreamNode);
                    }
                }
            }

            // Find all Edges that point to this RSB. Set their Target to the new Centralized BMP Node
            foreach (var edge in existingEdges.Where(x => x.TargetID == existingRSBNodeID))
            {
                edge.TargetID = newCentralizedBMPNodeID;
            }


            if (!centralizedBMPHasDownstreamBMP)
            {
                // Don't create a new node for this centralized BMP if it's has a downstream BMP, because its node 
                // was already created in MakeUpstreamBMPNodesAndEdges
                // Don't create an edge from this Centralized BMP to the downstream Regional Subbasin, since it already
                // has a downstream BMP whose node was created in MakeUpstreamBMPNodesAndEdges

                centralizedBMPNodes.Add(new Node
                { ID = newCentralizedBMPNodeID, TreatmentBMPID = rsbCentralizedBMPPairing.TreatmentBMPID });

                // is this not the same thing as edgeFromRegionalSubbasinToDownstreamRegionalSubbasin != null?
                if (downstreamRegionalSubbasinID != null)
                {
                    centralizedBMPEdges.Add(new Edge() { SourceID = newCentralizedBMPNodeID, TargetID = downstreamRegionalSubbasinID });
                }
            }
        }
    }
}