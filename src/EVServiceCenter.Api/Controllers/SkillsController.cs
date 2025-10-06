using System;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class SkillsController : ControllerBase
{
	private readonly ISkillService _skillService;
	public SkillsController(ISkillService skillService)
	{
		_skillService = skillService;
	}

	[HttpGet]
	[AllowAnonymous]
	public async Task<IActionResult> GetAll()
	{
		var data = await _skillService.GetAllAsync();
		return Ok(new { success = true, data });
	}

	[HttpGet("{id:int}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetById(int id)
	{
		var s = await _skillService.GetByIdAsync(id);
		if (s == null) return NotFound(new { success = false, message = "Kỹ năng không tồn tại" });
		return Ok(new { success = true, data = s });
	}

	[HttpPost]
	public async Task<IActionResult> Create([FromBody] SkillCreateRequest request)
	{
		if (!ModelState.IsValid)
		{
			var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
			return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
		}
		try
		{
			var created = await _skillService.CreateAsync(request);
			return Ok(new { success = true, data = created, message = "Tạo kỹ năng thành công" });
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { success = false, message = ex.Message });
		}
	}

	[HttpPut("{id:int}")]
	public async Task<IActionResult> Update(int id, [FromBody] SkillUpdateRequest request)
	{
		if (!ModelState.IsValid)
		{
			var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
			return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors });
		}
		try
		{
			var updated = await _skillService.UpdateAsync(id, request);
			return Ok(new { success = true, data = updated, message = "Cập nhật kỹ năng thành công" });
		}
		catch (ArgumentException ex)
		{
			return BadRequest(new { success = false, message = ex.Message });
		}
	}

	[HttpDelete("{id:int}")]
	public async Task<IActionResult> Delete(int id)
	{
		await _skillService.DeleteAsync(id);
		return Ok(new { success = true, message = "Xóa kỹ năng thành công" });
	}
}


