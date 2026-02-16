using Microsoft.SemanticKernel;

namespace FableCraft.Infrastructure.Llm;

public interface IKernelBuilder
{
    Microsoft.SemanticKernel.IKernelBuilder Create();

    PromptExecutionSettings GetDefaultPromptExecutionSettings();

    PromptExecutionSettings GetDefaultFunctionPromptExecutionSettings();
}
