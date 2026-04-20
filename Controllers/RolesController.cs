using BasisBank.Identity.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BasisBank.Identity.Api.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase {
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public RolesController(RoleManager<IdentityRole<int>> roleManager, UserManager<ApplicationUser> userManager) {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles() {
            var roles = _roleManager.Roles.Select(r => new { r.Id, r.Name }).ToList();
            return Ok(roles);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] string roleName) {
            if (string.IsNullOrWhiteSpace(roleName))
                return BadRequest("Role name required.");
            if (await _roleManager.RoleExistsAsync(roleName))
                return Conflict("Role exists.");

            var res = await _roleManager.CreateAsync(new IdentityRole<int>(roleName));
            if (!res.Succeeded)
                return StatusCode(500, string.Join(", ", res.Errors.Select(e => e.Description)));
            return CreatedAtAction(nameof(GetRoles), new { name = roleName }, null);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignRole([FromQuery] string userName, [FromQuery] string role) {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
                return NotFound("User not found.");
            if (!await _roleManager.RoleExistsAsync(role))
                return NotFound("Role not found.");

            var res = await _userManager.AddToRoleAsync(user, role);
            if (!res.Succeeded)
                return StatusCode(500, string.Join(", ", res.Errors.Select(e => e.Description)));
            return Ok("Role assigned.");
        }

        [HttpPost("revoke")]
        public async Task<IActionResult> RevokeRole([FromQuery] string userName, [FromQuery] string role) {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
                return NotFound("User not found.");

            var res = await _userManager.RemoveFromRoleAsync(user, role);
            if (!res.Succeeded)
                return StatusCode(500, string.Join(", ", res.Errors.Select(e => e.Description)));
            return Ok("Role revoked.");
        }
    }
}