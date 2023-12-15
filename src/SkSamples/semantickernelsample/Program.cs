using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.ImageGeneration;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planners;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.TemplateEngine;
using semantickernelsample.NativePlugIns;
using semantickernelsample.Skills;
using System.Collections.Concurrent;
using System.Text.Json;

internal class Program
{
    private static async Task Main(string[] args)
    {
        //await Sample_HelloSk();
        //await Sample_NativeFunctionsDirectInvoke();
        //await Sample_InvokeNativeFunctionsWithArguments();
        //await Sample_HelloPipeline();

        //await Sample_HelloCompletion1();
        //await Sample_HelloCompletion2();

        //await Sample_HelloInlineSemanticFunction();
        //await Sample_HelloSemnticFunction();
        //await Sample_HelloSemanticFunctionWithParams();
        //await Sample_SemanticTextTranslation();
        //await Sample_NestedSemanticFunction();   

        //await Sample_SemanticFunctionInvokesNativeFunction();
        //await Sample_SemanticMathOperationExtractor();
        //await Sample_NativeFunctionInvokesSemanticFunction();
        //await Sample_ChainingSemanticFunction();

        //await Sample_GroundingWithNativeSkill();
        //await Sample_StateMachine();
        //await Sample_SemanticSkills();

        //await Sample_SequencePlaner();
        await Sample_FictionPlaner();
    }


    /// <summary>
    /// Demonstrates the initialization of the kernel and execution of the single function in the pipeline. 
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_HelloSk()
    {
        IKernel kernel = GetKernel();

        var time = kernel.ImportFunctions(new TimePlugin());

        var result = await kernel.RunAsync(time["Today"]);

        var time2 = kernel.ImportFunctions(new MyDateTimePlugin());

        result = await kernel.RunAsync(time2["Now"]);

        result = await kernel.RunAsync(time["UtcNow"]);

        Console.WriteLine(result);
    }


    /// <summary>
    /// Demonstrats how to import native functions from the skill and how to execute them directly.
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_NativeFunctionsDirectInvoke()
    {
        IKernel kernel = new KernelBuilder().Build();

        SKContext ctx = kernel.CreateNewContext();

        var functions = kernel.ImportFunctions(new MyDateTimePlugin());

        var time = await functions["Now"].InvokeAsync(ctx);

        Console.WriteLine(time);

        var today = await functions["Today"].InvokeAsync(ctx);

        Console.WriteLine(today);
    }


    /// <summary>
    /// Demonstrates how to invoke the native function with arguments.
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_InvokeNativeFunctionsWithArguments()
    {
        IKernel kernel = new KernelBuilder().Build();

        SKContext ctx = kernel.CreateNewContext();

        var functions = kernel.ImportFunctions(new SamplePlugIn());

        var variables = new ContextVariables
        {
            ["arg1"] = "121",
            ["arg2"] = "234",
        };

        // Get the GetIntent function from the OrchestratorPlugin and run it
        var result = await kernel.RunAsync(variables, functions["AddNumbersFunction"]);

        Console.WriteLine(result);
    }




    /// <summary>
    /// Demonstrates the SK pipeline mechanism.
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_HelloPipeline()
    {
        IKernel kernel = new KernelBuilder().Build();

        var functions = kernel.ImportFunctions(new StringSkill());

        // Execute two functions in the pipeline.
        KernelResult result = await kernel.RunAsync("  Nothing special in this prompt   ",
                                                     functions["ToUpper"], functions["Trim"]);

        // How to execute the long list of function.
        result = await kernel.RunAsync("Nothing special in this prompt", functions.Values.ToArray());

        Console.WriteLine(result);
    }


    /// <summary>
    /// Sample method that demonstrates the use of the Azure Semantic Kernel.
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_HelloCompletion1()
    {
        IKernel kernel = GetKernel();

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


    /// <summary>
    /// Demonsrates completions with Semantc Kernel.
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_HelloCompletion2()
    {
        IKernel kernel = GetKernel();

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



    /// <summary>
    /// Demonstrates the inline semantic function.
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_HelloInlineSemanticFunction()
    {
        OpenAIRequestSettings requestSettings = new()
        {
            ExtensionData = {
                {"MaxTokens", 500},
                {"Temperature", 0.5},
                {"TopP", 0.0}, // Diversity coeff. https://arxiv.org/pdf/2306.13840.pdf
                {"PresencePenalty", 0.0},
                {"FrequencyPenalty", 0.0}
            }
        };

        var kernel = GetKernel();

        string prompt = @"Bot: How can I help you?
                        User: {{$input}}
                        ---------------------------------------------
                        The intent of the user in 5 words or less: ";

        var getIntentFunction = kernel.CreateSemanticFunction(prompt, requestSettings, "MyItentPlugIn");

        string ask = "I want to post a real at instagram about our research project in the last 7 months. The real should be 2 minutes long.";

        var result = await kernel.RunAsync(ask, getIntentFunction);

        Console.WriteLine(result);
    }


    /// <summary>
    /// Demonstrates the semantic function loaded from file.
    /// It first loads the function SimplifyAbstract and translates the simplified version.
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_HelloSemnticFunction()
    {
        var kernel = GetKernel();

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins");

        // Import the OrchestratorPlugin from the plugins directory.
        var orchestratorPlugin = kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "SamplePlugIn");

        // Get the GetIntent function from the OrchestratorPlugin and run it
        var result = await kernel.RunAsync(paperAbstract,
            orchestratorPlugin["SimplifyAbstract"], orchestratorPlugin["Translator"]);

        Console.WriteLine(result);
    }

    private const string paperAbstract = @"ABSTRACT Sparse representation has attracted much attention from researchers in fields of signal
processing, image processing, computer vision, and pattern recognition. Sparse representation also has a
good reputation in both theoretical research and practical applications. Many different algorithms have been
proposed for sparse representation. The main purpose of this paper is to provide a comprehensive study
and an updated review on sparse representation and to supply guidance for researchers. The taxonomy of
sparse representation methods can be studied from various viewpoints. For example, in terms of different
norm minimizations used in sparsity constraints, the methods can be roughly categorized into five groups:
1) sparse representation with l0-norm minimization; 2) sparse representation with lp-norm (0 < p < 1)
minimization; 3) sparse representation with l1-norm minimization; 4) sparse representation with l2,1-norm
minimization; and 5) sparse representation with l2-norm minimization. In this paper, a comprehensive
overview of sparse representation is provided. The available sparse representation algorithms can also be
empirically categorized into four groups: 1) greedy strategy approximation; 2) constrained optimization;
3) proximity algorithm-based optimization; and 4) homotopy algorithm-based sparse representation. The
rationales of different algorithms in each category are analyzed and a wide range of sparse representation
applications are summarized, which could sufficiently reveal the potential nature of the sparse representation
theory. In particular, an experimentally comparative study of these sparse representation algorithms was
presented.";


    /// <summary>
    /// Demonstrates semantic functions with parameters (variables). The bot first asks the user about his age and then
    /// the user enters the age. The bot decribes the paper abstract in the way that is understandable for the given age.
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_HelloSemanticFunctionWithParams()
    {
        var kernel = GetKernel();

        var functions = kernel.ImportFunctions(new SamplePlugIn());

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins");

        // Import the OrchestratorPlugin from the plugins directory.
        var orchestratorPlugin = kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "SamplePlugIn");

        var variables = new ContextVariables
        {
            ["input"] = paperAbstract,
            ["history"] = @"Bot: How old are you?",
            ["age"] = "15",
            ["options"] = "7, 10, 15, 20, 30"
        };

        // Get the GetIntent function from the OrchestratorPlugin and run it
        var result = await kernel.RunAsync(variables, functions["NullFUnction"], orchestratorPlugin["SimplifyAbstractWithParams"]);

        Console.WriteLine(result);
    }


    /// <summary>
    /// Demonstrates semantic functions with output parameters (variables).
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_SemanticTextTranslation()
    {
        var kernel = GetKernel();

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins");

        // Import the OrchestratorPlugin from the plugins directory.
        var semanticFunctions = kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "SamplePlugIn");

        var variables = new ContextVariables
        {
            ["input"] = paperAbstract,
            ["language"] = @"bosnian"
        };

        // Translates the text.
        var result = await kernel.RunAsync(variables, semanticFunctions["Translator"]);

        Console.WriteLine(result);
    }

    /// <summary>
    /// Demonstrates semantic function invokes another semantic function.
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_NestedSemanticFunction()
    {
        var kernel = GetKernel();

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins");

        // Import the OrchestratorPlugin from the plugins directory.
        var semanticFunctions = kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "SamplePlugIn");

        var variables = new ContextVariables
        {
            ["input"] = paperAbstract,
            ["language"] = "german"
        };

        // Get the GetIntent function from the OrchestratorPlugin and run it
        var result = await kernel.RunAsync(variables, semanticFunctions["SimplifyAbstractWithParamsAndTranslate"]);

        Console.WriteLine(result);
    }


    /// <summary>
    /// Invookes the semantic function that extracts the name of the mathematical operation in user intent. 
    /// Additionally, this function also extract all specified numerical values, that should be used as the input of the function.
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_SemanticMathOperationExtractor()
    {
        var kernel = GetKernel();

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins");

        // Import the OrchestratorPlugin from the plugins directory.
        var semanticFunctions = kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "SamplePlugIn");

        var functions = kernel.ImportFunctions(new SamplePlugIn());

        var variables = new ContextVariables
        {
            ["input"] = "Execute the function exponent with following arguments 42 and 7",
        };

        // Get the GetIntent function from the OrchestratorPlugin and run it
        var result = await kernel.RunAsync(variables, semanticFunctions["MathOperationExtractor"]);

        Console.WriteLine(result);
    }

    /// <summary>
    /// Demonstrates how native function invokes another semantic function.
    /// The semantic function extracts the name of the mathematical operation in user intent and the list of numerical values.
    /// Returned extracted values (name of the operation and list of numerics) are used inside native function.
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_NativeFunctionInvokesSemanticFunction()
    {
        var kernel = GetKernel();

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins");

        // Import the OrchestratorPlugin from the plugins directory.
        var semanticFunctions = kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "SamplePlugIn");

        var nativeFunctions = kernel.ImportFunctions(new SamplePlugIn(kernel));

        var variables = new ContextVariables
        {
            ["input"] = "Execute the function exponent with following arguments 2 and 16",
        };

        // Get the GetIntent function from the OrchestratorPlugin and run it
        var result = await kernel.RunAsync(variables, nativeFunctions["ExecuteMathOperationFunction"]);

        Console.WriteLine(result);
    }



    /// <summary>
    /// Demonstrates semantic function invokes another native function.
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_SemanticFunctionInvokesNativeFunction()
    {
        var kernel = GetKernel();

        var functions = kernel.ImportFunctions(new StringSkill(), "StringPlugin");

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins");

        // Import the OrchestratorPlugin from the plugins directory.
        var semanticFunctions = kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "SamplePlugIn");

        var variables = new ContextVariables
        {
            ["input"] = paperAbstract,
            ["language"] = "bosnian"
        };

        // Get the GetIntent function from the OrchestratorPlugin and run it
        var result = await kernel.RunAsync(variables, semanticFunctions["AbstractWordCounter"]);

        Console.WriteLine(result);
    }

    /// <summary>
    /// Execute functions in the chain.
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_ChainingSemanticFunction()
    {
        var kernel = GetKernel();

        string myJokePrompt = """
Tell a short joke about {{$input}}.
""";
        string myPoemPrompt = """
Take this "{{$input}}" and convert it to a nursery rhyme.
""";
        string myMenuPrompt = """
Make this poem "{{$input}}" influence the three items in a coffee shop menu. 
The menu reads in enumerated form:

""";
        OpenAIRequestSettings sett = new()
        {
            ExtensionData = {
                {"MaxTokens", 500},
                {"Temperature", 0.5},
                {"TopP", 0.0},
                {"PresencePenalty", 0.0},
                {"FrequencyPenalty", 0.0}
            }
        };

        //var settings = new new PromptTemplateConfig { ModelSettings = new  maxTokens = 500 };
        var myJokeFunction = kernel.CreateSemanticFunction(myJokePrompt, requestSettings: sett);
        var myPoemFunction = kernel.CreateSemanticFunction(myPoemPrompt, requestSettings: sett);
        var myMenuFunction = kernel.CreateSemanticFunction(myMenuPrompt, requestSettings: sett);

        // Get the GetIntent function from the OrchestratorPlugin and run it
        var result = await kernel.RunAsync(new ContextVariables("Damir Dobric Microsoft Regional Director"),
            myJokeFunction, myPoemFunction, myMenuFunction);

        Console.WriteLine(result);
    }



    /// <summary>
    /// Using native skill for grounding.
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_GroundingWithNativeSkill()
    {
        IKernel kernel = GetKernel();

        string skPrompt = @"
        You have a knowledge of international days.

        Is date {{$input}} known for something?
        ";

        kernel.ImportFunctions(new MyDateTimePlugin(), "datetime");
        kernel.ImportFunctions(new SamplePlugIn(), "sample");

        var sematicFunc = kernel.CreateSemanticFunction(skPrompt, new OpenAIRequestSettings { MaxTokens = 150 });

        var result = await kernel.RunAsync(new ContextVariables(DateTime.Now.ToString("MMM/dd")!), sematicFunc);

        Console.WriteLine(result);

        skPrompt = @"
        You have a knowledge of international days.

        {{sample.Enrich}}

        Is date {{$input}} known for something? Also provide most useful historical data for at least two popular events at that day.
        ";

        sematicFunc = kernel.CreateSemanticFunction(skPrompt, new OpenAIRequestSettings { MaxTokens = 150 });

        result = await kernel.RunAsync(new ContextVariables(DateTime.Now.ToString("MMM/dd")!), sematicFunc);

        Console.WriteLine(result);
    }

    private static async Task Sample_StateMachine()
    {
        IKernel kernel = GetKernel();

        string skPrompt = @"
        {{sample.AddValue}}
        {{sample.AddValue}}
        Tell me about the meaning of the number  {{$mystate}}.
      {{$available_functions}}
        ";

        kernel.ImportFunctions(new SamplePlugIn(), "sample");

        var variables = new ContextVariables("Today is: ");
        variables.Set("mystate", "running...");

        var semanticFunc = kernel.CreateSemanticFunction(skPrompt, new OpenAIRequestSettings { MaxTokens = 150 });

        var result = await kernel.RunAsync(variables, semanticFunc);

        Console.WriteLine(result);
    }

    private static async Task Sample_SemanticSkills()
    {
        IKernel kernel = GetKernel();

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


    /// <summary>
    /// Demonstrates semantic function invokes another native function.
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_SequencePlaner()
    {
        var kernel = GetKernel();

        var functions = kernel.ImportFunctions(new semantickernelsample.NativePlugIns.MathPlugin(), "MathPlugin");

        var planner = new SequentialPlanner(kernel);

        //var ask = "If my investment of 2130.23 dollars increased by 23%, how much would I have after I spent $5 on a latte?";
        var ask = "Wenn meine Investition von 2130,23 Dollar um 23% gestiegen ist, wie viel hätte ich, nachdem ich 5 Dollar für einen Latte ausgegeben habe?";
        //var ask = "Calculate the sum of numbers, 1,2,3,4,5,6,7 and then divide it by number of elements in the list.";
        //var ask = "Calculate the energy of the 10kg ball falling from sky at the moment of hitting the surface, if tha ball started at 10km height with the start speed of 100m/s.";
        //var ask = "Calculates the quadrat of the sum of first 10 numbers.";

        var plan = await planner.CreatePlanAsync(ask);

        Console.WriteLine("Plan:\n");
        Console.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));

        // Execute the plan
        var result = await kernel.RunAsync(plan);

        Console.WriteLine("Plan results:");
        Console.WriteLine(result.GetValue<string>()!.Trim());
    }

    /// <summary>
    /// Demonstrates semantic function invokes another native function.
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_FictionPlaner()
    {
        var kernel = GetKernel();

        var mathFunctions = kernel.ImportFunctions(new semantickernelsample.NativePlugIns.MathPlugin(), "MathPlugin");
        var sampleFunctions = kernel.ImportFunctions(new SamplePlugIn(), "SamplePlugin");

        var planner = new SequentialPlanner(kernel);

        var ask = "Please calculate the fiction between the stone and alpha centaury with the contraction jumping of 150 kobasica.";

        var plan = await planner.CreatePlanAsync(ask);

        Console.WriteLine("Plan:\n");
        Console.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));

        // Execute the plan
        var result = await kernel.RunAsync(plan);

        Console.WriteLine("Plan results:");
        Console.WriteLine(result.GetValue<string>()!.Trim());
    }
    #region Helpers

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


    private static IKernel GetKernel()
    {
        return GetOpenAIKernel();
        //return GetAzureKernel();
    }


    private static IKernel GetOpenAIKernel(string? useChatOrTextCompletionModel = "chat")
    {
        IKernel kernel;

        if (useChatOrTextCompletionModel == null || useChatOrTextCompletionModel == "chat")
        {
            kernel = new KernelBuilder()
             .WithOpenAIChatCompletionService(
            Environment.GetEnvironmentVariable("OPENAI_CHATCOMPLETION_DEPLOYMENT")!, // The name of your deployment (e.g., "gpt-3.5-turbo")
            Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
            Environment.GetEnvironmentVariable("OPENAI_ORGID")!)
        .Build();
        }
        else
            throw new Exception("Text Completion Models are deprected.");

        return kernel;
    }

    private static IKernel GetAzureKernel(string? useChatOrTextCompletionModel = "chat")
    {
        IKernel kernel;

        if (useChatOrTextCompletionModel == null || useChatOrTextCompletionModel == "chat")
        {
            kernel = new KernelBuilder()
            .WithAzureOpenAIChatCompletionService(
                Environment.GetEnvironmentVariable("AZURE_OPENAI_CHATCOMPLETION_DEPLOYMENT")!,  // The name of your deployment (e.g., "text-davinci-003")
                Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,    // The endpoint of your Azure OpenAI service
                Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!      // The API key of your Azure OpenAI service
            )
            .Build();
        }
        else
        {
            kernel = new KernelBuilder()
            .WithAzureTextCompletionService(
          Environment.GetEnvironmentVariable("AZURE_OPENAI_TEXTCOMPLETION_DEPLOYMENT")!,  // The name of your deployment (e.g., "text-davinci-003")
          Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,    // The endpoint of your Azure OpenAI service
          Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!      // The API key of your Azure OpenAI service
      )
      .Build();
        }
        return kernel;
    }
    #endregion
}

