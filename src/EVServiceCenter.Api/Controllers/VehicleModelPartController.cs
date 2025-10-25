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
public class VehicleModelPartController : ControllerBase
{
    private readonly IVehicleModelPartService _vehicleModelPartService;

    public VehicleModelPartController(IVehicleModelPartService vehicleModelPartService)
    {
        _vehicleModelPartService = vehicleModelPartService;
    }

    /// <summary>
    /// Get parts by vehicle model ID
    /// </summary>
    [HttpGet("model/{modelId}")]
    public async Task<ActionResult<IEnumerable<VehicleModelPartResponse>>> GetPartsByModelId(int modelId)
    {
        try
        {
            var parts = await _vehicleModelPartService.GetPartsByModelIdAsync(modelId);
            return Ok(parts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Get models by part ID
    /// </summary>
    [HttpGet("part/{partId}")]
    public async Task<ActionResult<IEnumerable<VehicleModelPartResponse>>> GetModelsByPartId(int partId)
    {
        try
        {
            var models = await _vehicleModelPartService.GetModelsByPartIdAsync(partId);
            return Ok(models);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }



    /// <summary>
    /// Create vehicle model part relationship
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<VehicleModelPartResponse>> Create([FromBody] CreateVehicleModelPartRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var modelPart = await _vehicleModelPartService.CreateAsync(request);
            return CreatedAtAction(nameof(GetPartsByModelId), new { modelId = modelPart.ModelId }, modelPart);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Update vehicle model part relationship
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<VehicleModelPartResponse>> Update(int id, [FromBody] UpdateVehicleModelPartRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var modelPart = await _vehicleModelPartService.UpdateAsync(id, request);
            return Ok(modelPart);
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
    /// Delete vehicle model part relationship
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var result = await _vehicleModelPartService.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Vehicle model part relationship not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

}
