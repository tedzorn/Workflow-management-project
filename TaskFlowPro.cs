using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TaskFlowPro
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TaskFlowPro")); //switch to sql Server or postgresql for prod

            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddSignalR(); //for real-time updates
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<NotificationHub>("/notificationHub"); //signalR endpoint
            });
        }
    }

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TaskItem> Tasks { get; set; }
    }

    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public PriorityLevel Priority { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //track creation date
        public DateTime? CompletedAt { get; set; } //track completion date
    }

    public enum PriorityLevel
    {
        Low,
        Medium,
        High
    }

    public class NotificationHub : Microsoft.AspNetCore.SignalR.Hub
    {
        //hub for real-time updates
    }
}

//controller
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace TaskFlowPro.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public TasksController(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            return await _context.Tasks.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItem>> GetTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            return task;
        }

        [HttpPost]
        public async Task<ActionResult<TaskItem>> CreateTask(TaskItem task)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("TaskCreated", task); //notify clients

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, TaskItem task)
        {
            if (id != task.Id) return BadRequest();

            _context.Entry(task).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("TaskUpdated", task); //notify clients

            return NoContent();
        }

        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            task.IsCompleted = true;
            task.CompletedAt = DateTime.UtcNow;
            _context.Entry(task).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("TaskCompleted", task); //notify clients
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("TaskDeleted", id); //notify clients

            return NoContent();
        }
    }
}
