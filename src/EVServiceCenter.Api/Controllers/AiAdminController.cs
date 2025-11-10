using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EVServiceCenter.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVServiceCenter.Api.Controllers
{
	[ApiController]
	[Route("api/admin/rag")]
	[Authorize(Policy = "StaffOrAdmin")]
	public class AiAdminController : ControllerBase
	{
		private readonly IAiServiceClient _ai;

		public AiAdminController(IAiServiceClient ai)
		{
			_ai = ai;
		}

		[HttpPost("ingest")]
		public async Task<IActionResult> IngestUrl([FromForm] string? url, [FromForm(Name = "metadata_json")] string? metadataJson, [FromForm(Name = "created_by")] string? createdBy, CancellationToken ct)
		{
			var resp = await _ai.IngestUrlAsync(url, metadataJson, createdBy, ct);
			return await Passthrough(resp, ct);
		}

		[HttpPost("ingest-sitemap")]
		public async Task<IActionResult> IngestSitemap([FromQuery] string url, [FromQuery(Name = "metadata_json")] string? metadataJson, [FromQuery(Name = "created_by")] string? createdBy, CancellationToken ct)
		{
			var resp = await _ai.IngestSitemapAsync(url, metadataJson, createdBy, ct);
			return await Passthrough(resp, ct);
		}

		[HttpPost("ingest-files")]
		[RequestSizeLimit(1024L * 1024L * 100L)]
		public async Task<IActionResult> IngestFiles([FromForm] List<IFormFile> files, [FromForm(Name = "metadata_json")] string? metadataJson, [FromForm(Name = "created_by")] string? createdBy, CancellationToken ct)
		{
			var toSend = new List<(Stream, string, string)>();
			foreach (var f in files)
			{
				toSend.Add((f.OpenReadStream(), f.FileName, f.ContentType ?? "application/octet-stream"));
			}
			var resp = await _ai.IngestFilesAsync(toSend, metadataJson, createdBy, ct);
			return await Passthrough(resp, ct);
		}

		[HttpPost("ingest-images")]
		[RequestSizeLimit(1024L * 1024L * 100L)]
		public async Task<IActionResult> IngestImages([FromForm] List<IFormFile> files, [FromForm(Name = "metadata_json")] string? metadataJson, [FromForm(Name = "created_by")] string? createdBy, CancellationToken ct)
		{
			var toSend = new List<(Stream, string, string)>();
			foreach (var f in files)
			{
				toSend.Add((f.OpenReadStream(), f.FileName, f.ContentType ?? "application/octet-stream"));
			}
			var resp = await _ai.IngestImagesAsync(toSend, metadataJson, createdBy, ct);
			return await Passthrough(resp, ct);
		}

		[HttpGet("documents")]
		public async Task<IActionResult> ListDocuments([FromQuery] string? status, [FromQuery(Name = "role_access")] string? roleAccess, [FromQuery(Name = "center_id")] int? centerId, [FromQuery] string? lang, [FromQuery] int? limit, [FromQuery] int? offset, CancellationToken ct)
		{
			var resp = await _ai.ListDocumentsAsync(status, roleAccess, centerId, lang, limit, offset, ct);
			return await Passthrough(resp, ct);
		}

		[HttpGet("documents/{documentId:guid}")]
		public async Task<IActionResult> GetDocument([FromRoute] Guid documentId, CancellationToken ct)
		{
			var resp = await _ai.GetDocumentAsync(documentId, ct);
			return await Passthrough(resp, ct);
		}

		[HttpPut("documents/{documentId:guid}")]
		public async Task<IActionResult> UpdateDocument([FromRoute] Guid documentId, [FromBody] object body, [FromHeader(Name = "X-Updated-By")] string? updatedBy, CancellationToken ct)
		{
			var json = body is string s ? s : System.Text.Json.JsonSerializer.Serialize(body);
			var content = new StringContent(json, Encoding.UTF8, "application/json");
			var resp = await _ai.UpdateDocumentAsync(documentId, content, updatedBy, ct);
			return await Passthrough(resp, ct);
		}

		[HttpDelete("documents/{documentId:guid}")]
		public async Task<IActionResult> DeleteDocument([FromRoute] Guid documentId, [FromHeader(Name = "X-Deleted-By")] string? deletedBy, CancellationToken ct)
		{
			var resp = await _ai.DeleteDocumentAsync(documentId, deletedBy, ct);
			return await Passthrough(resp, ct);
		}

		[HttpPost("documents/{documentId:guid}/restore")]
		public async Task<IActionResult> RestoreDocument([FromRoute] Guid documentId, [FromHeader(Name = "X-Updated-By")] string? updatedBy, CancellationToken ct)
		{
			var resp = await _ai.RestoreDocumentAsync(documentId, updatedBy, ct);
			return await Passthrough(resp, ct);
		}

		private static async Task<IActionResult> Passthrough(HttpResponseMessage resp, CancellationToken ct)
		{
			var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/json";
			var body = await resp.Content.ReadAsStringAsync(ct);
			return new ContentResult
			{
				StatusCode = (int)resp.StatusCode,
				Content = body,
				ContentType = contentType
			};
		}
	}
}


