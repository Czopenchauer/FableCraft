namespace FableCraft.Application.Exceptions;

public class AdventureNotFoundException(Guid guid) : Exception($"Adventure with ID {guid} was not found.");