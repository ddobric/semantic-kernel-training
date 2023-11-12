using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.ImageGeneration;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using semantickernelsample.Skills;
using System.Collections.Concurrent;

internal class Program
{
    private static async Task Main(string[] args)
    {
        await Sample_HelloCompletion();
        //await Sample_Completion2();
        //await Sample_NativeSKills();
        //await Sample_NativeSkillPipeline();
        //await Sample_GroundingWithNativeSkill();
        await Sample_StateMachine();
        //await Sample_SemanticSkills();
    }

    private static async Task Sample_HelloCompletion()
    {
        IKernel kernel = GetAzureKernel();

        string prompt = @"
        {{$input}}

        Give me the TLDR in 5 words.
        ";

        var semanticFunction = kernel.CreateSemanticFunction(prompt);

        string systemMessage = @"
        1) A robot may not injure a human being or, through inaction,
        allow a human being to come to harm.

        2) A robot must obey orders given it by human beings except where
        such orders would conflict with the First Law.

        3) A robot must protect its own existence as long as such protection
        does not conflict with the First or Second Law.
        ";

        var summary = await kernel.RunAsync(systemMessage, semanticFunction);

        Console.WriteLine(summary);
    }

    private static async Task Sample_Completion2()
    {
        IKernel kernel = GetAzureKernel();

        var prompt = @"{{$input}}

        One line TLDR with the fewest words.";

        var semanticFunc = kernel.CreateSemanticFunction(prompt);

        string text1 = @"
1st Law of Thermodynamics - Energy cannot be created or destroyed.
2nd Law of Thermodynamics - For a spontaneous process, the entropy of the universe increases.
3rd Law of Thermodynamics - A perfect crystal at zero Kelvin has zero entropy.";

        string text2 = @"
1. An object at rest remains at rest, and an object in motion remains in motion at constant speed and in a straight line unless acted on by an unbalanced force.
2. The acceleration of an object depends on the mass of the object and the amount of force applied.
3. Whenever one object exerts a force on another object, the second object exerts an equal and opposite on the first.";

        Console.WriteLine(await semanticFunc.InvokeAsync(text1, kernel));

        Console.WriteLine(await semanticFunc.InvokeAsync(text2, kernel));
    }

    private static async Task Sample_NativeSKills()
    {
        IKernel kernel = new KernelBuilder().Build();

        var nativeSkills = kernel.ImportSkill(new DateTimeSkill());

        var time = await nativeSkills["Now"].InvokeAsync();

        var today = await nativeSkills["Today"].InvokeAsync();

        var builtInSkill = kernel.ImportSkill(new TimeSkill());
    }

    private static async Task Sample_NativeSkillPipeline()
    {
        IKernel kernel = new KernelBuilder().Build();

        var nativeSkills = kernel.ImportSkill(new DateTimeSkill());

        SKContext result = await kernel.RunAsync("Nothing special in this prompt",
          nativeSkills["Today"], nativeSkills["Now"]);

        Console.WriteLine(result);
    }

    /// <summary>
    /// Using native skill for grounding.
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_GroundingWithNativeSkill()
    {
        IKernel kernel = GetAzureKernel();

        string skPrompt = @"
        You have a knowledge of international days.

        Is date {{datetime.Today}} known for something?
        ";

        kernel.ImportFunctions(new DateTimeSkill(), "datetime");
        kernel.ImportFunctions(new SampleSkill(), "sample");

        var sematicFunc = kernel.CreateSemanticFunction(skPrompt, new OpenAIRequestSettings { MaxTokens = 150 });

        var result = await kernel.RunAsync(sematicFunc);

        Console.WriteLine(result);

        skPrompt = @"
        You have a knowledge of international days.

        {{sample.Enrich}}


        Is date {{datetime.Today}} known for something? Also provide most useful historical data for at least two popular events at that day.
        ";

        sematicFunc = kernel.CreateSemanticFunction(skPrompt, new OpenAIRequestSettings { MaxTokens = 150 });

        result = await kernel.RunAsync(sematicFunc);

        Console.WriteLine(result);
    }

    private static async Task Sample_StateMachine()
    {
        IKernel kernel = GetAzureKernel();

        string skPrompt = @"
        {{sample.AddValue}}
        {{sample.AddValue}}
        Tell me about the meaning of the number  {{$mystate}}.
      {{$available_functions}}
        ";

        kernel.ImportFunctions(new SampleSkill(), "sample");

        var variables = new ContextVariables("Today is: ");

        var semanticFunc = kernel.CreateSemanticFunction(skPrompt, new OpenAIRequestSettings { MaxTokens = 150 });

        var result = await kernel.RunAsync(variables, semanticFunc);

        Console.WriteLine(result);
    }

    private static async Task Sample_SemanticSkills()
    {
        IKernel kernel = GetAzureKernel();

        string prompt1 = @"
We develop tailor-made software solutions that are right for you and ones that are based on your ideas. Our experienced team of internationally recognised experts will help you plan and implement your solution. For our clients, this means: we support you with integrated solutions for important topics such as cloud, 
digitalisation, Industry 4.0, IoT, machine learning, development of mobile apps, mixed reality or system integration.
We develop tailor-made software solutions that are right for you and ones that are based on your ideas. Our experienced team of internationally recognised experts will help you plan and implement your solution. For our clients, this means: we support you with integrated solutions for important topics such as cloud, digitalisation, 
Industry 4.0, IoT, machine learning, development of mobile apps, mixed reality or system integration.We are your Microsoft Azure Gold partner and will support you in the implementation and migration of cloud solutions. With our technical expertise, we can help you tackle existing obstacles and optimise the real advantages of the cloud for your digital transformation.

Customized cloud solutions for Microsoft Azure with technological and economic persuasiveness
Increasing use of resources means risk of fluctuations in capacity within your company’s internal data centre. Associated with this are, among other things, immense restriction in flexibility and access to essential business applications and continuously increasing operating costs. Protecting your IT landscape by shifting availability, load peaks or traffic to Microsoft Azure or the cloud is, from a technological and economic point of view, a sensible measure.

daenet paves the way to the cloud for you and is your partner when it comes to quickly and effectively realising the benefits of using Microsoft Azure and capitalising from them now and in the future.

Services
We develop a migration strategy and a cloud/digital strategy with you
We help you find a hybrid solution (combining between the cloud and on premises)
We migrate the existing solution to the cloud (lift & shift)
We modernise the existing solution and migrate it to the cloud (application modernisation)
Procedure
We proceed iteratively. Based on our many years of experience, we define three-month work packages. Larger projects can also be divided into such manageable blocks. We, of course, work in an agile way and can adapt such work packages to your wishes and possibilities. This means that the first scenarios can start online productively almost immediately.

An example that has been successfully implemented many times: You want to migrate a solution to the cloud.

More efficiency for your cloud projects
We offer you our profound cloud knowledge as standardized best-practice service packages including visioning/initial workshops, assessment, design, implementation, operation and support of your chosen solution,. The aim is to accelerate your cloud projects, make them a reality, train and relieve your development teams, and give you the freedom to concentrate on your professional expertise and the development of your digital business.";


        kernel.ImportSemanticSkillFromDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SemanticSkills"), "SummarizeSkill");

        var functions = kernel.Functions.GetFunctionViews();
        //ConcurrentDictionary<string, List<FunctionView>> nativeFunctions = functions.Where(fnc=>fnc.NativeFunctions;
        //ConcurrentDictionary<string, List<FunctionView>> semanticFunctions = functions.SemanticFunctions;

        foreach (var view in functions)
        {
            Console.WriteLine("Skill: " + view.Name);
           // foreach (FunctionView func in view.) { PrintFunction(func); }
        }

        var semanticFunc = kernel.Functions.GetFunction("SummarizeSkill", "Summarize");

        var result = await kernel.RunAsync(prompt1, semanticFunc);

        Console.WriteLine(result);
    }


    private static void PrintFunction(FunctionView func)
    {
        Console.WriteLine($"   {func.Name}: {func.Description}");

        if (func.Parameters.Count > 0)
        {
            Console.WriteLine("      Params:");
            foreach (var p in func.Parameters)
            {
                Console.WriteLine($"      - {p.Name}: {p.Description}");
                Console.WriteLine($"        default: '{p.DefaultValue}'");
            }
        }

        Console.WriteLine();
    }

    private static IKernel GetOpenAIKernel()
    {
        var kernel = Kernel.Builder
        .WithOpenAIChatCompletionService(
            Environment.GetEnvironmentVariable("OPENAI_DEPLOYMENT")!, // The name of your deployment (e.g., "gpt-3.5-turbo")
            Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
            Environment.GetEnvironmentVariable("OPENAI_ORGID")!
        )
        .Build();

        return kernel;
    }

    private static IKernel GetAzureKernel()
    {
        var kernel = Kernel.Builder
        .WithAzureTextCompletionService(
            Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT")!,  // The name of your deployment (e.g., "text-davinci-003")
            Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,    // The endpoint of your Azure OpenAI service
            Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!      // The API key of your Azure OpenAI service
        )
        .Build();

        return kernel;
        //var kernel = Kernel.Builder.Build();
        ////AddAzureOpenAICompletionBackend
        //kernel.Config.AddAzureTextCompletionService(
        //    "davinci-backend",                   // Alias used by the kernel
        //    "text-davinci-003-damir-andreas",    // Azure OpenAI *Deployment ID*
        //    Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"), // Azure OpenAI *Endpoint*
        //    Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")  // Azure OpenAI *Key*
        //);

        //return kernel;
    }


}

