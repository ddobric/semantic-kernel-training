using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.KernelExtensions;
using Microsoft.SemanticKernel.Orchestration;
using semantickernelsample.Skills;

internal class Program
{
    private static async Task Main(string[] args)
    {
        //await Sample1();
        //await Sample2();
        //await Sample3_ImportNativeFunctionSkills();
        //await Sample3_BuildSKillPipeline();
        //await Sample4_UsingVariables();
        await Sample5_StateMachine();
    }

    private static async Task Sample1()
    {
        IKernel kernel = GetKernel();

        string skPrompt = @"
        {{$input}}

        Give me the TLDR in 5 words.
        ";

        var tldrFunction = kernel.CreateSemanticFunction(skPrompt);

        string textToSummarize = @"
        1) A robot may not injure a human being or, through inaction,
        allow a human being to come to harm.

        2) A robot must obey orders given it by human beings except where
        such orders would conflict with the First Law.

        3) A robot must protect its own existence as long as such protection
        does not conflict with the First or Second Law.
        ";

        var summary = await kernel.RunAsync(textToSummarize, tldrFunction);

        Console.WriteLine(summary);
    }

    private static async Task Sample2()
    {
        IKernel kernel = GetKernel();

        var prompt = @"{{$input}}

One line TLDR with the fewest words.";

        var summarize = kernel.CreateSemanticFunction(prompt);

        string text1 = @"
1st Law of Thermodynamics - Energy cannot be created or destroyed.
2nd Law of Thermodynamics - For a spontaneous process, the entropy of the universe increases.
3rd Law of Thermodynamics - A perfect crystal at zero Kelvin has zero entropy.";

        string text2 = @"
1. An object at rest remains at rest, and an object in motion remains in motion at constant speed and in a straight line unless acted on by an unbalanced force.
2. The acceleration of an object depends on the mass of the object and the amount of force applied.
3. Whenever one object exerts a force on another object, the second object exerts an equal and opposite on the first.";

        Console.WriteLine(await summarize.InvokeAsync(text1));

        Console.WriteLine(await summarize.InvokeAsync(text2));
    }

    private static async Task Sample3_ImportNativeFunctionSkills()
    {
        IKernel kernel = new KernelBuilder().Build();

        var nativeSkills = kernel.ImportSkill(new DateTimeSkill());

        var time = await nativeSkills["Now"].InvokeAsync();

        var today = await nativeSkills["Today"].InvokeAsync();

        var defaultTimeSkills = kernel.ImportSkill(new TimeSkill());
    }

    private static async Task Sample3_BuildSKillPipeline()
    {
        IKernel kernel = new KernelBuilder().Build();

        var nativeSkills = kernel.ImportSkill(new DateTimeSkill());

        SKContext result = await kernel.RunAsync("Nothing special in this prompt", 
          /* nativeSkills["Today"]*/ nativeSkills["Now"]);

        Console.WriteLine(result);
    }

    private static async Task Sample4_UsingVariables()
    {
        IKernel kernel = GetKernel();

        string skPrompt = @"
        You have a knowledge of international days.
        {{sample.Enrich}}

        Is date {{datetime.Today}} known for something?
        ";

        kernel.ImportSkill(new DateTimeSkill(), "datetime");
        kernel.ImportSkill(new SampleSkill(), "sample");

        var day = kernel.CreateSemanticFunction(skPrompt, maxTokens: 150);

        var result = await kernel.RunAsync(day);

        Console.WriteLine(result);
    }

    private static async Task Sample5_StateMachine()
    {
        IKernel kernel = GetKernel();

        string skPrompt = @"
        {{sample.AddValue}}
        {{sample.AddValue}}
        Tell me about the meaning of the number  {{$mystate}}.
      {{$available_functions}}
        ";
          
        kernel.ImportSkill(new SampleSkill(), "sample");

        var semanticFunc = kernel.CreateSemanticFunction(skPrompt, maxTokens: 150);

        var result = await kernel.RunAsync(semanticFunc);

        Console.WriteLine(result);
    }

    private static IKernel GetKernel()
    {
        var kernel = Kernel.Builder.Build();
        //AddAzureOpenAICompletionBackend
        kernel.Config.AddAzureTextCompletionService(
            "davinci-backend",                   // Alias used by the kernel
            "text-davinci-003-damir-andreas",    // Azure OpenAI *Deployment ID*
            Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"), // Azure OpenAI *Endpoint*
            Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")  // Azure OpenAI *Key*
        );

        return kernel;
    }


}

// Output => Protect humans, follow orders, survive.