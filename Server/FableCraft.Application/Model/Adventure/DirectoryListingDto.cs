namespace FableCraft.Application.Model.Adventure;

public class DirectoryListingDto
{
    public required string CurrentPath { get; init; }
    public string? ParentPath { get; init; }
    public required DirectoryEntryDto[] Directories { get; init; }
}

public class DirectoryEntryDto
{
    public required string FullPath { get; init; }
    public required string Name { get; init; }
}
