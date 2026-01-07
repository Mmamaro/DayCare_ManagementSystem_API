
using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Repositories;
using DayCare_ManagementSystem_API.Service;
using System.Net.Http;
using System.Runtime.CompilerServices;
using static QRCoder.PayloadGenerator;

namespace ccod_voyc_integration_workerservice.Services
{
    public class PickUpWorkerService : BackgroundService
    {
        private readonly ILogger<PickUpWorkerService> _logger;
        private readonly IEvent _eventRepo;
        private readonly IStudent _studetRepo;
        private readonly EmailService _emailService;
        private int count;
        private int hour;
        private int minutes;
        TimeOnly runtime = new TimeOnly();

        public PickUpWorkerService(ILogger<PickUpWorkerService> logger, IStudent studetRepo, IEvent eventRepo, EmailService emailService)
        {
            _logger = logger;
            _studetRepo = studetRepo;
            _eventRepo = eventRepo;
            _emailService = emailService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            while (!stoppingToken.IsCancellationRequested)
            {

                //var tokens = await _voycService.GetTokens();

                var delay = GetWaitTime();

                _logger.LogInformation($"Next run is at: {DateTime.Now.Add(delay).AddHours(2)}");

                if (delay.TotalMinutes > 0)
                {
                    await Task.Delay(delay, stoppingToken);

                }

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                var filter = new EventFilter()
                {
                    EndDate = DateTime.Today.AddHours(17),
                    StartDate = DateTime.Today.AddHours(08)
                };

                var events = await _eventRepo.GetEventsByFilters(filter);

                var groupedEvents = events.GroupBy(x => x.StudentId);

                if (groupedEvents.Any())
                {
                    foreach (var group in groupedEvents)
                    {
                        if(group.Count() < 2)
                        {

                            var firstEvent = group.First();

                            var student = await _studetRepo.GetStudentById(firstEvent.StudentId);

                            if ( student != null)
                            {
                                var baseUrl = Environment.GetEnvironmentVariable("FrontEndBaseUrl")!;
                                var url = $"{baseUrl}/auth";
                                var path = $"./Templates/PickUpNotification.html";
                                var template = System.IO.File.ReadAllText(path).Replace("\n", "");

                                template = template.Replace("{{StudentName}}", student.StudentProfile.FirstName + " " +
                                            student.StudentProfile.LastName);

                                var receipients = student.NextOfKins.Select(x => x.Email).ToList(); ;

                                await _emailService.SendTemplateEmail(receipients, "Pickup Notification", template);
                            }
                            else
                            {
                                _logger.LogWarning("Student returned empty in the PickUpWorkerService and this is not supposed to happen.");
                            }
                        }
                    }
                }
            }
        }

        private TimeSpan GetWaitTime()
        {
            hour = int.Parse(Environment.GetEnvironmentVariable("PickUpWorkerRunHour") ?? "17");
            minutes = int.Parse(Environment.GetEnvironmentVariable("PickUpWokerRunMinutes") ?? "00");
            runtime = new TimeOnly(hour, minutes);
            var currentTime = TimeOnly.FromDateTime(DateTime.Now.AddHours(2));

            // If the target time is later today
            if (currentTime <= runtime.AddMinutes(2))
            {
                return runtime.ToTimeSpan() - currentTime.ToTimeSpan();
            }

            // If the target time has already passed today, schedule for tomorrow
            return runtime.ToTimeSpan().Add(TimeSpan.FromHours(24)) - currentTime.ToTimeSpan();
        }
    }
}
