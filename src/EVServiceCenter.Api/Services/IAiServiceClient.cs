using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EVServiceCenter.Api.Services
{
	public interface IAiServiceClient
	{
		Task<HttpResponseMessage> IngestUrlAsync(string? url, string? metadataJson, string? createdBy, CancellationToken ct = default);
		Task<HttpResponseMessage> IngestSitemapAsync(string url, string? metadataJson, string? createdBy, CancellationToken ct = default);
		Task<HttpResponseMessage> IngestFilesAsync(IEnumerable<(Stream Content, string FileName, string ContentType)> files, string? metadataJson, string? createdBy, CancellationToken ct = default);
		Task<HttpResponseMessage> IngestImagesAsync(IEnumerable<(Stream Content, string FileName, string ContentType)> files, string? metadataJson, string? createdBy, CancellationToken ct = default);

		Task<HttpResponseMessage> ListDocumentsAsync(string? status, string? roleAccess, int? centerId, string? lang, int? limit, int? offset, CancellationToken ct = default);
		Task<HttpResponseMessage> GetDocumentAsync(Guid documentId, CancellationToken ct = default);
		Task<HttpResponseMessage> UpdateDocumentAsync(Guid documentId, HttpContent body, string? updatedBy, CancellationToken ct = default);
		Task<HttpResponseMessage> DeleteDocumentAsync(Guid documentId, string? deletedBy, CancellationToken ct = default);
		Task<HttpResponseMessage> RestoreDocumentAsync(Guid documentId, string? updatedBy, CancellationToken ct = default);
	}
}


