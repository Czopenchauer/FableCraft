using FableCraft.Application.NarrativeEngine;

using Microsoft.AspNetCore.Mvc;

namespace FableCraft.Server.Controllers;

/// <summary>
/// Controller for scene image generation and management.
/// </summary>
[ApiController]
[Route("api/Play/{adventureId:guid}/scene/{sceneId:guid}/images")]
public class SceneImageController : ControllerBase
{
    private readonly ISceneImageService _sceneImageService;

    public SceneImageController(ISceneImageService sceneImageService)
    {
        _sceneImageService = sceneImageService;
    }

    /// <summary>
    /// Gets all images for a scene.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SceneImageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SceneImageDto>>> GetImages(
        Guid adventureId,
        Guid sceneId,
        CancellationToken cancellationToken)
    {
        var images = await _sceneImageService.GetImagesForSceneAsync(
            adventureId, sceneId, cancellationToken);
        return Ok(images);
    }

    /// <summary>
    /// Generates a new image for a scene.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SceneImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SceneImageDto>> GenerateImage(
        Guid adventureId,
        Guid sceneId,
        CancellationToken cancellationToken)
    {
        try
        {
            var image = await _sceneImageService.GenerateImageAsync(
                adventureId, sceneId, cancellationToken);
            return Ok(image);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Selects a specific image version as the active image for the scene.
    /// </summary>
    [HttpPost("{imageId:guid}/select")]
    [ProducesResponseType(typeof(SceneImageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SceneImageDto>> SelectImage(
        Guid adventureId,
        Guid sceneId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        try
        {
            var image = await _sceneImageService.SelectImageAsync(
                adventureId, sceneId, imageId, cancellationToken);
            return Ok(image);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a specific image version.
    /// </summary>
    [HttpDelete("{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteImage(
        Guid adventureId,
        Guid sceneId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _sceneImageService.DeleteImageAsync(
                adventureId, sceneId, imageId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
