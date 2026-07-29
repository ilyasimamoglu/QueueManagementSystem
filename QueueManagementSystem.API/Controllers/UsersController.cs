using Microsoft.AspNetCore.Mvc;
using QueueManagementSystem.Business;
using QueueManagementSystem.Entities;
using System;

namespace QueueManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        // Dependency Injection ile UserManager'ı içeri alıyoruz
        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _userService.GetAllUsers();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var user = _userService.GetUserById(id);
            if (user == null)
                return NotFound(new { message = "Kullanıcı bulunamadı." });

            return Ok(user);
        }

        [HttpPost]
        public IActionResult Add([FromBody] User user)
        {
            try
            {
                // İş katmanına gönderiyoruz, hata varsa catch bloğuna düşecek
                _userService.AddUser(user);
                return Ok(new { message = "Kullanıcı başarıyla eklendi." });
            }
            catch (Exception ex)
            {
                // İş katmanında yazdığımız fırlatılan hataları (Exception) yakalayıp HTTP 400 Bad Request olarak dönüyoruz
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] User user)
        {
            try
            {
                user.Id = id; // Güvenlik önlemi: URL'deki ID ile nesnenin ID'sini eşitliyoruz
                _userService.UpdateUser(user);
                return Ok(new { message = "Kullanıcı bilgileri güncellendi." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _userService.DeleteUser(id);
            return Ok(new { message = "Kullanıcı sistemden silindi." });
        }

        // --- Gişe Görevlisi veya Admin Girişi İçin Özel Metot ---
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto loginDto)
        {
            var user = _userService.AuthenticateUser(loginDto.Username, loginDto.Password);

            if (user == null)
            {
                // Kullanıcı yoksa veya pasifse 401 Unauthorized dönüyoruz
                return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı, ya da hesap pasif." });
            }

            return Ok(user);
        }
    }

    // Login işlemi için dışarıdan sadece kullanıcı adı ve şifre alacağımız Data Transfer Object (DTO)
    public class LoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}