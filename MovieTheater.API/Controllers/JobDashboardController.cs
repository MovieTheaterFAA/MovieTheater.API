using Microsoft.AspNetCore.Mvc;
using Quartz;
using Quartz.Impl.Matchers;

namespace MovieTheater.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobDashboardController : ControllerBase
    {
        private readonly ISchedulerFactory _schedulerFactory;

        public JobDashboardController(ISchedulerFactory schedulerFactory)
        {
            _schedulerFactory = schedulerFactory;
        }

        [HttpGet("jobs")]
        public async Task<IActionResult> GetJobs()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobGroups = await scheduler.GetJobGroupNames();
            var jobs = new List<object>();

            foreach (var group in jobGroups)
            {
                var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(group));
                foreach (var jobKey in jobKeys)
                {
                    var triggers = await scheduler.GetTriggersOfJob(jobKey);
                    foreach (var trigger in triggers)
                    {
                        var triggerState = await scheduler.GetTriggerState(trigger.Key);
                        jobs.Add(new
                        {
                            JobName = jobKey.Name,
                            Group = group,
                            Trigger = trigger.Key.Name,
                            TriggerType = trigger.GetType().Name,
                            NextFireTime = trigger.GetNextFireTimeUtc()?.ToLocalTime(),
                            PreviousFireTime = trigger.GetPreviousFireTimeUtc()?.ToLocalTime(),
                            State = triggerState.ToString()
                        });
                    }
                }
            }

            return Ok(jobs);
        }
    }
}