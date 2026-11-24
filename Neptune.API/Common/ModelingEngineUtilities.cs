using System;
using Hangfire;
using Neptune.EFModels.Entities;
using Neptune.Jobs.Hangfire;
using NetTopologySuite.Geometries;

namespace Neptune.API.Common
{
    public static class ModelingEngineUtilities
    {
        public static async System.Threading.Tasks.Task QueueLGURefreshForArea(Geometry? oldShape, Geometry? newShape, NeptuneDbContext dbContext)
        {
            Geometry loadGeneratingUnitRefreshAreaGeometry;

            if (oldShape == null && newShape == null)
            {
                throw new InvalidOperationException("At least one input to QueueLGURefreshForArea must not be null.");
            }

            if (oldShape == null)
            {
                loadGeneratingUnitRefreshAreaGeometry = newShape;
            }
            else if (newShape == null)
            {
                loadGeneratingUnitRefreshAreaGeometry = oldShape;
            }
            else
            {
                loadGeneratingUnitRefreshAreaGeometry = oldShape.Union(newShape);
            }

            // NPT-1158: ProcessDate stays empty until the refresh has finished (the Overlay API sets it).
            // It used to be set here as well, which made a refresh that failed or never ran look the
            // same as one that completed.
            var loadGeneratingUnitRefreshArea = new LoadGeneratingUnitRefreshArea()
            {
                LoadGeneratingUnitRefreshAreaGeometry = loadGeneratingUnitRefreshAreaGeometry,
            };

            await dbContext.LoadGeneratingUnitRefreshAreas.AddAsync(loadGeneratingUnitRefreshArea);
            await dbContext.SaveChangesAsync();

            BackgroundJob.Enqueue<LoadGeneratingUnitRefreshJob>(x => x.RunJob(loadGeneratingUnitRefreshArea.LoadGeneratingUnitRefreshAreaID));
        }
    }
}
