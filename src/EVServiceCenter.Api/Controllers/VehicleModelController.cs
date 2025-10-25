using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehicleModelController : ControllerBase
{
    private readonly IVehicleModelService _vehicleModelService;

    public VehicleModelController(IVehicleModelService vehicleModelService)
    {
        _vehicleModelService = vehicleModelService;
    }

    /// <summary>
    /// Get all vehicle models
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleModelResponse>>> GetAll()
    {
        try
        {
            var models = await _vehicleModelService.GetAllAsync();
            return Ok(models);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Get vehicle model by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<VehicleModelResponse>> GetById(int id)
    {
        try
        {
            var model = await _vehicleModelService.GetByIdAsync(id);
            return Ok(model);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    // Brand removed - endpoint deleted

    /// <summary>
    /// Get active vehicle models
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<VehicleModelResponse>>> GetActive()
    {
        try
        {
            var models = await _vehicleModelService.GetActiveModelsAsync();
            return Ok(models);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }


    /// <summary>
    /// Create new vehicle model
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<VehicleModelResponse>> Create([FromBody] CreateVehicleModelRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var model = await _vehicleModelService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = model.ModelId }, model);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Update vehicle model
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<VehicleModelResponse>> Update(int id, [FromBody] UpdateVehicleModelRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var model = await _vehicleModelService.UpdateAsync(id, request);
            return Ok(model);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete vehicle model
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var result = await _vehicleModelService.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Vehicle model not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

}
