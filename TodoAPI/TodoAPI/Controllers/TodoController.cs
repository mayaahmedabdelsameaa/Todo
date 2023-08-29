using HR_Management_System.DTO.CustomResult;
using Microsoft.AspNetCore.Mvc;
using TodoAPI.Domain.Interfaces;
using TodoAPI.Domain.Models;
using AutoMapper;
using TodoAPI.DTOS;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;

namespace TodoAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TodoController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpClientFactory _httpClientFactory;
        public TodoController(IUnitOfWork unitOfWork, IMapper mapper, IHttpClientFactory httpClientFactory)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetTodos()
        {
            try
            {
                var todos = await GetTodosAsync();
                return Ok(todos);

            }
            catch (Exception ex)
            {
                return BadRequest("Can not fetch data");
            }
        }

        private async Task<string> GetTodosAsync()
        {
            var client = _httpClientFactory.CreateClient("todos");
            var response = await client.GetStringAsync("todos");
            return response;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTodoById(int id)
        {
            if (id == null)
            {
                return StatusCode(500, "You must enter the id");
            }

            Todo _todo = await _unitOfWork.Todo.GetByIdAsync(id);
            if(_todo == null)
            {
                return NotFound("The entered id is not exist");
            }
            TodoDTO todo = _mapper.Map<Todo, TodoDTO>(_todo); 
            return Ok(todo);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTodo(TodoDTO _todo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Error creating todo");
            }

            Todo todo = _mapper.Map<TodoDTO, Todo>(_todo);

            // Parse the token and retrieve the userId
            var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            // Call a method to retrieve the decrypted userId from the token
            var userId = ExtractUserIdFromToken(accessToken);
            todo.CustomUserId = userId;
            
            var result = await _unitOfWork.Todo.AddAsync(todo);
            
            _unitOfWork.Complete();
            
            if (result != null){
                return Ok("Todo is created successfully");
            }
            
            return BadRequest("Error creating todo, check the data entered");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTodo(int id, TodoDTO _todo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Error");
            }
            
            var fetchTodo = await _unitOfWork.Todo.GetByIdAsync(id);
            
            if (fetchTodo == null)
            {
                return BadRequest("The chosen Todo not exist");
            }

            _todo.id = fetchTodo.Id;
            _todo.userId = fetchTodo.CustomUserId;
            Todo todo = _mapper.Map<TodoDTO, Todo>(_todo);
            
            var result = _unitOfWork.Todo.UpdateAsync(id, todo);
            
            _unitOfWork.Complete();
            
            if (result != null)
            {
                return Ok("Todo is updated successfully");
            }
            
            return BadRequest("Error updating todo, check the data entered");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodo(int id)
        {
            var fetchTodo = await GetTodoById(id);
            
            if (fetchTodo == null)
            {
                return BadRequest("The chosen Todo not exist");
            }
            
            var result = _unitOfWork.Todo.DeleteAsync(id);
            
            _unitOfWork.Complete();
            
            if(result != null)
            {
                return Ok("Todo Deleted Successfully");
            }
            
            return BadRequest("Error Deleting todo");
        }

        [HttpGet("fetchDb")]
        public async Task<IActionResult> GetAllCreatedTodos([FromQuery] int userId)
        {
            var fetchedtodos = await _unitOfWork.Todo.GetTodosByUserId(userId);
            List<TodoDTO> todos = new List<TodoDTO>();
            if (fetchedtodos == null)
            {
                return Ok();
            }

            foreach (var item in fetchedtodos)
            {
                TodoDTO todo = _mapper.Map<Todo, TodoDTO>(item);
                todos.Add(todo);
            }
            return Ok(todos);
        }
        private int ExtractUserIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var userIdClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "userId");

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            // Return a default value or throw an exception as needed
            throw new ApplicationException("Invalid token or missing user ID claim.");
        }
    
    }
}
