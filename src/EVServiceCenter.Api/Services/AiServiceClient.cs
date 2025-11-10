using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EVServiceCenter.Api.Configurations;
using Microsoft.Extensions.Options;

namespace EVServiceCenter.Api.Services
{
	public class AiServiceClient : IAiServiceClient
	{
		private readonly HttpClient _http;
		private readonly AiServiceOptions _options;

		public AiServiceClient(HttpClient http, IOptions<AiServiceOptions> options)
		{
			_http = http;
			_options = options.Value ?? new AiServiceOptions();

			if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
			{
				_http.BaseAddress = new Uri(_options.BaseUrl!.TrimEnd('/'));
			}

			if (_options.TimeoutSeconds > 0)
			{
				_http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
			}
		}

		private void AddInternalToken()
		{
			_http.DefaultRequestHeaders.Remove("X-Internal-Token");
			if (!string.IsNullOrWhiteSpace(_options.InternalToken))
			{
				_http.DefaultRequestHeaders.Add("X-Internal-Token", _options.InternalToken);
			}
		}

		public Task<HttpResponseMessage> IngestUrlAsync(string? url, string? metadataJson, string? createdBy, CancellationToken ct = default)
		{
			AddInternalToken();
			var form = new MultipartFormDataContent();
			if (!string.IsNullOrWhiteSpace(url)) form.Add(new StringContent(url), "url");
			if (!string.IsNullOrWhiteSpace(metadataJson)) form.Add(new StringContent(metadataJson, Encoding.UTF8, "application/json"), "metadata_json");
			if (!string.IsNullOrWhiteSpace(createdBy)) form.Add(new StringContent(createdBy), "created_by");
			return _http.PostAsync("/admin/ingest", form, ct);
		}

		public Task<HttpResponseMessage> IngestSitemapAsync(string url, string? metadataJson, string? createdBy, CancellationToken ct = default)
		{
			AddInternalToken();
			var qp = new List<string> { $"url={Uri.EscapeDataString(url)}" };
			if (!string.IsNullOrWhiteSpace(metadataJson)) qp.Add($"metadata_json={Uri.EscapeDataString(metadataJson)}");
			if (!string.IsNullOrWhiteSpace(createdBy)) qp.Add($"created_by={Uri.EscapeDataString(createdBy)}");
			var qs = string.Join("&", qp);
			return _http.PostAsync($"/admin/ingest-sitemap?{qs}", content: null, ct);
		}

		public Task<HttpResponseMessage> IngestFilesAsync(IEnumerable<(Stream Content, string FileName, string ContentType)> files, string? metadataJson, string? createdBy, CancellationToken ct = default)
		{
			AddInternalToken();
			var form = new MultipartFormDataContent();
			foreach (var f in files)
			{
				var sc = new StreamContent(f.Content);
				if (!string.IsNullOrWhiteSpace(f.ContentType))
				{
					sc.Headers.ContentType = new MediaTypeHeaderValue(f.ContentType);
				}
				form.Add(sc, "files", f.FileName);
			}
			if (!string.IsNullOrWhiteSpace(metadataJson)) form.Add(new StringContent(metadataJson, Encoding.UTF8, "application/json"), "metadata_json");
			if (!string.IsNullOrWhiteSpace(createdBy)) form.Add(new StringContent(createdBy), "created_by");
			return _http.PostAsync("/admin/ingest-files", form, ct);
		}

		public Task<HttpResponseMessage> IngestImagesAsync(IEnumerable<(Stream Content, string FileName, string ContentType)> files, string? metadataJson, string? createdBy, CancellationToken ct = default)
		{
			AddInternalToken();
			var form = new MultipartFormDataContent();
			foreach (var f in files)
			{
				var sc = new StreamContent(f.Content);
				if (!string.IsNullOrWhiteSpace(f.ContentType))
				{
					sc.Headers.ContentType = new MediaTypeHeaderValue(f.ContentType);
				}
				form.Add(sc, "files", f.FileName);
			}
			if (!string.IsNullOrWhiteSpace(metadataJson)) form.Add(new StringContent(metadataJson, Encoding.UTF8, "application/json"), "metadata_json");
			if (!string.IsNullOrWhiteSpace(createdBy)) form.Add(new StringContent(createdBy), "created_by");
			return _http.PostAsync("/admin/ingest-images", form, ct);
		}

		public Task<HttpResponseMessage> ListDocumentsAsync(string? status, string? roleAccess, int? centerId, string? lang, int? limit, int? offset, CancellationToken ct = default)
		{
			AddInternalToken();
			var qp = new List<string>();
			if (!string.IsNullOrWhiteSpace(status)) qp.Add($"status={Uri.EscapeDataString(status)}");
			if (!string.IsNullOrWhiteSpace(roleAccess)) qp.Add($"role_access={Uri.EscapeDataString(roleAccess)}");
			if (centerId.HasValue) qp.Add($"center_id={centerId.Value}");
			if (!string.IsNullOrWhiteSpace(lang)) qp.Add($"lang={Uri.EscapeDataString(lang)}");
			if (limit.HasValue) qp.Add($"limit={limit.Value}");
			if (offset.HasValue) qp.Add($"offset={offset.Value}");
			var qs = qp.Any() ? "?" + string.Join("&", qp) : string.Empty;
			return _http.GetAsync($"/admin/documents{qs}", ct);
		}

		public Task<HttpResponseMessage> GetDocumentAsync(Guid documentId, CancellationToken ct = default)
		{
			AddInternalToken();
			return _http.GetAsync($"/admin/documents/{documentId}", ct);
		}

		public Task<HttpResponseMessage> UpdateDocumentAsync(Guid documentId, HttpContent body, string? updatedBy, CancellationToken ct = default)
		{
			AddInternalToken();
			if (!string.IsNullOrWhiteSpace(updatedBy))
			{
				_http.DefaultRequestHeaders.Remove("X-Updated-By");
				_http.DefaultRequestHeaders.Add("X-Updated-By", updatedBy);
			}
			return _http.PutAsync($"/admin/documents/{documentId}", body, ct);
		}

		public Task<HttpResponseMessage> DeleteDocumentAsync(Guid documentId, string? deletedBy, CancellationToken ct = default)
		{
			AddInternalToken();
			if (!string.IsNullOrWhiteSpace(deletedBy))
			{
				_http.DefaultRequestHeaders.Remove("X-Deleted-By");
				_http.DefaultRequestHeaders.Add("X-Deleted-By", deletedBy);
			}
			return _http.DeleteAsync($"/admin/documents/{documentId}", ct);
		}

		public Task<HttpResponseMessage> RestoreDocumentAsync(Guid documentId, string? updatedBy, CancellationToken ct = default)
		{
			AddInternalToken();
			if (!string.IsNullOrWhiteSpace(updatedBy))
			{
				_http.DefaultRequestHeaders.Remove("X-Updated-By");
				_http.DefaultRequestHeaders.Add("X-Updated-By", updatedBy);
			}
			return _http.PostAsync($"/admin/documents/{documentId}/restore", content: null, ct);
		}
	}
}


