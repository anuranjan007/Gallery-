using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureFileManager.Application.Interfaces;

namespace SecureFileManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FoldersController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FoldersController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRootFolders(CancellationToken ct)
        {
            var folders = await _fileService.ListRootFoldersAsync(ct);
            return Ok(folders);
        }
    }
}
