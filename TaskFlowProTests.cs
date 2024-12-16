using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using TaskFlowPro;
using TaskFlowPro.Controllers;

namespace TaskFlowProTests
{
    [TestClass]
    public class TasksControllerTests
    {
        private TasksController _controller;
        private AppDbContext _context;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _controller = new TasksController(_context, null);
        }

        [TestMethod]
        public async Task CreateTask_ShouldAddTaskToDatabase()
        {
            var newTask = new TaskItem
            {
                Title = "Test Task",
                Description = "Description of Test Task",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = PriorityLevel.Medium
            };

            var result = await _controller.CreateTask(newTask);

            Assert.AreEqual(1, await _context.Tasks.CountAsync());
        }

        [TestMethod]
        public async Task GetTask_ShouldReturnCorrectTask()
        {
            var task = new TaskItem
            {
                Title = "Existing Task",
                Description = "Description",
                DueDate = DateTime.UtcNow,
                Priority = PriorityLevel.High
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            var result = await _controller.GetTask(task.Id);

            Assert.IsNotNull(result.Value);
            Assert.AreEqual(task.Title, result.Value.Title);
        }

        [TestMethod]
        public async Task DeleteTask_ShouldRemoveTaskFromDatabase()
        {
            var task = new TaskItem
            {
                Title = "Task to Delete",
                Description = "Description",
                DueDate = DateTime.UtcNow,
                Priority = PriorityLevel.Low
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            await _controller.DeleteTask(task.Id);

            Assert.AreEqual(0, await _context.Tasks.CountAsync());
        }
    }
}