using Microsoft.SemanticKernel;
//using Microsoft.SemanticKernel.AI.ImageGeneration;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using semantickernelsample.NativePlugIns;
using semantickernelsample.Skills;
using System.Text.Json;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Microsoft.SemanticKernel.ChatCompletion;
using Tiktoken;
using System.Diagnostics;
//using Microsoft.SemanticKernel.Planning.Handlebars;
internal class Program
{
    private static async Task Main(string[] args)
    {
        //TestPerformance();

        //WorkingWithTokens();

        //
        // The ultimate scenario
        //
        // await Sample_Lighting();

        //--------------------
        // NATIVE FUNCTIONS
        //--------------------

        //await Sample_HelloSk();
        //await Sample_NativeFunctionsWithArguments();
        //await Sample_InvokeNativeFunctionsWithArguments();
        //await Sample_HelloPipeline();

        //--------------------
        // SEMANTIC FUNCTIONS
        //--------------------
        //await Sample_InlineSemanticFunc1();
        //await Sample_InlineSemanticFunc2();
        //await Sample_InlineSemanticFunc3();

        //await Sample_SemanticFunc_SimplifyAbstract();
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

        //await TemplateExample();

        //await Sample_StepwisePlaner();

        await Sample_FictionWithFunctionCall();

        await Sample_FictionPlaner();

        await ES_BookHours();
    }


    private static void TestPerformance()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        int n = 10000;
        for (int i = 0; i < n; i++)
        {
            var kernel = GetKernel();
            var lightPlugin = kernel.ImportPluginFromObject(new LightPlugin());
      
            // Create chat history
            var history = new ChatHistory();

            // Get chat completion service
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        }

        sw.Stop();

        Console.WriteLine(sw.Elapsed);
        Console.WriteLine($"{(double)sw.ElapsedMilliseconds/(double)n} ms per instance.");
    }


    public static async Task Sample_Lighting()
    {
        var kernel = GetKernel();

        var lightPlugin = kernel.ImportPluginFromObject(new LightPlugin());

        var newState = lightPlugin["ChangeState"].InvokeAsync(kernel, new KernelArguments { ["newState"] = true });

        var state = lightPlugin["GetState"].InvokeAsync(kernel);

        // Create chat history
        var history = new ChatHistory();

        // Get chat completion service
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        // Start the conversation
        Console.Write("User > ");

        string? userInput;

        while ((userInput = Console.ReadLine()) != null)
        {
            // Add user input
            history.AddUserMessage(userInput);

            // Enable auto function calling
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            // Get the response from the AI
            var result = await chatCompletionService.GetChatMessageContentAsync(
                history,
                executionSettings: openAIPromptExecutionSettings,
                kernel: kernel);

            // Print the results
            Console.WriteLine("Assistant > " + result);

            // Add the message from the agent to the chat history
            history.AddMessage(result.Role, result.Content ?? string.Empty);

            // Get user input again
            Console.WriteLine("User > ");
        }
    }

    /// <summary>
    /// Demonstrates the initialization of the kernel and execution of the single function in the pipeline. 
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_HelloSk()
    {
        Kernel kernel = Kernel.CreateBuilder().Build();

        var time1 = kernel.ImportPluginFromObject(new MyDateTimePlugin());

        var res1 = await kernel?.InvokeAsync<string>(time1["Today"])!;

        FunctionResult res2 = await kernel?.InvokeAsync(time1["Today"])!;

        DateTime utc = await kernel!.InvokeAsync<DateTime>(time1["UtcNow"]);

        FunctionResult res3 = await kernel?.InvokeAsync(time1["DayOfWeek"])!;

        Console.WriteLine(utc);
    }


    /// <summary>
    /// Demonstrats how to import native functions from the skill and how to execute them directly.
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_NativeFunctionsWithArguments()
    {
        Kernel kernel = Kernel.CreateBuilder().Build();

        var plugin = kernel.ImportPluginFromObject(new MyDateTimePlugin());

        KernelArguments args = new KernelArguments
        {
            ["planet"] = "jupiter"
        };

        var time1 = await kernel.InvokeAsync<string>(plugin["TodayOnPlanet"], args);

        Console.WriteLine(time1);

        var today = await plugin["Today"].InvokeAsync(kernel, args);

        Console.WriteLine(today);
    }


    /// <summary>
    /// Demonstrates how to invoke the native function with arguments.
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_InvokeNativeFunctionsWithArguments()
    {
        Kernel kernel = GetKernel();

        var plugin = kernel.CreatePluginFromObject(new SamplePlugIn());

        var variables = new KernelArguments
        {
            ["arg1"] = "121",
            ["arg2"] = "234",
        };

        // Get the GetIntent function from the OrchestratorPlugin and run it
        var result = await kernel.InvokeAsync(plugin["AddNumbersFunction"], variables);

        Console.WriteLine(result);
    }




    /// <summary>
    /// Demonstrates the SK pipeline mechanism.
    /// </summary>
    /// <returns></returns>
    //private static async Task Sample_HelloPipeline()
    //{
    //    Kernel kernel = Kernel.CreateBuilder().Build();

    //    var stringPlugin = kernel.CreatePluginFromObject(new StringSkill());

    //    var promptFunc = kernel.CreateFunctionFromPrompt("  Nothing special in this prompt   ");

    //    KernelFunction pipeline = KernelFunctionCombinators.Pipe(new[] { promptFunc, stringPlugin["ToUpper"], stringPlugin["Trim"] }, "pipeline");

    //    // Execute two functions in the pipeline.
    //    FunctionResult result = await kernel.InvokeAsync(promptFunc, );

    //    // How to execute the long list of function.
    //    result = await kernel.RunAsync("Nothing special in this prompt", stringPlugin.Values.ToArray());

    //    Console.WriteLine(result);
    //}


    /// <summary>
    /// Sample method that demonstrates the use of the Azure Semantic Kernel.
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_InlineSemanticFunc1()
    {
        Kernel kernel = GetKernel();

        string prompt = @"
        {{$input}}

        Give me the TLDR in 5 words.
        ";

        var semanticFunction = kernel.CreateFunctionFromPrompt(prompt, new OpenAIPromptExecutionSettings() { MaxTokens = 100, Temperature = 0.4, TopP = 1 });

        string systemMessage = @"
        1) A robot may not injure a human being or, through inaction,
        allow a human being to come to harm.

        2) A robot must obey orders given it by human beings except where
        such orders would conflict with the First Law.

        3) A robot must protect its own existence as long as such protection
        does not conflict with the First or Second Law.
        ";

        KernelArguments args = new() { ["input"] = systemMessage };

        var summary = await kernel.InvokeAsync(semanticFunction, args);

        Console.WriteLine(summary);
    }


    /// <summary>
    /// Demonsrates completions with Semantc Kernel.
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_InlineSemanticFunc2()
    {
        Kernel kernel = GetKernel();

        var prompt = @"{{$input}}

        One line TLDR with the fewest words.";

        var semanticFunc = kernel.CreateFunctionFromPrompt(prompt);

        string text1 = @"
        1st Law of Thermodynamics - Energy cannot be created or destroyed.
        2nd Law of Thermodynamics - For a spontaneous process, the entropy of the universe increases.
        3rd Law of Thermodynamics - A perfect crystal at zero Kelvin has zero entropy.";

        string text2 = @"
        1. An object at rest remains at rest, and an object in motion remains in motion at constant speed and in a straight line unless acted on by an unbalanced force.
        2. The acceleration of an object depends on the mass of the object and the amount of force applied.
        3. Whenever one object exerts a force on another object, the second object exerts an equal and opposite on the first.";

        Console.WriteLine(await kernel.InvokeAsync<string>(semanticFunc, new() { ["input"] = text1 }));

        Console.WriteLine(await kernel.InvokeAsync<string>(semanticFunc, new() { ["input"] = text2 }));
    }



    /// <summary>
    /// Demonstrates the inline semantic function with settings.
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_InlineSemanticFunc3()
    {
        OpenAIPromptExecutionSettings requestSettings = new OpenAIPromptExecutionSettings() { MaxTokens = 1000, Temperature = 0.4, TopP = 1 };

        var kernel = GetKernel();

        string prompt = @"Bot: How can I help you?
                        User: {{$a}}, {{$b}}, {{$c}}, {{$d}}, {{$e}}
                        ---------------------------------------------
                        Calculate the anomally between given consumtion values. Provide very short explanation and the final result. Tell me which value is an anomaly.";

        var getIntentFunction = kernel.CreateFunctionFromPrompt(prompt, requestSettings, "MyItentPlugIn");

        string ask = "Following numbers represent the consumtion in Azure: {{$a}}, {{$b}}, {{$c}}, {{$d}}, {{$e}}";

        var result = await kernel.InvokeAsync(getIntentFunction, 
            new() { 
                //["input"]=ask,
                ["a"] = 1200,
                ["b"] = 1207,
                ["c"] = 1190,
                ["d"] = 2200,
                ["e"] = 1199
            });

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
    /// Demonstrates the semantic function loaded from file.
    /// It first loads the function SimplifyAbstract and translates the simplified version.
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_SemanticFunc_SimplifyAbstract()
    {
        var kernel = GetKernel();

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins/SamplePlugin");

        // Import the OrchestratorPlugin from the plugins directory.
        var samplePlugin = kernel.ImportPluginFromPromptDirectory(pluginsDirectory, "SamplePlugin");

        var simlifiedText = await kernel.InvokeAsync(samplePlugin["SimplifyAbstract"], new() { ["input"] = paperAbstract });

        Console.WriteLine($"{simlifiedText}");
        Console.WriteLine();

        var translatedAndSimplified = await kernel.InvokeAsync(samplePlugin["Translator"], new() { ["input"] = simlifiedText, ["language"] = "croatian" });
    
        Console.WriteLine(translatedAndSimplified);
    }



    /// <summary>
    /// Demonstrates semantic functions with parameters (variables). The bot first asks the user about his age and then
    /// the user enters the age. The bot decribes the paper abstract in the way that is understandable for the given age.
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_HelloSemanticFunctionWithParams()
    {
        var kernel = GetKernel();

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins/SamplePlugin");

        // Import the OrchestratorPlugin from the plugins directory.
        var samplePlugin = kernel.ImportPluginFromPromptDirectory(pluginsDirectory, "SamplePlugIn");

        var variables = new KernelArguments
        {
            ["input"] = paperAbstract,
            ["history"] = @"Bot: How old are you?",
            ["age"] = "15",
            ["options"] = "7, 10, 15, 20, 30"
        };

        var simpleSbstract = await kernel.InvokeAsync<string>(samplePlugin["SimplifyAbstractWithParams"], variables);

        Console.WriteLine(simpleSbstract);
    }


    /// <summary>
    /// Demonstrates semantic functions with output parameters (variables).
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_SemanticTextTranslation()
    {
        var kernel = GetKernel();

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins/SamplePlugin");


        // Import the OrchestratorPlugin from the plugins directory.
        var samplePlugin = kernel.CreatePluginFromPromptDirectory(pluginsDirectory, "SamplePlugIn");


        var variables = new KernelArguments()
        {
            ["input"] = paperAbstract,
            ["language"] = "german",
        };

        var translatorPlugIn = samplePlugin["Translator"];

        // Translates the text.
        var result = await kernel.InvokeAsync(samplePlugin["Translator"], variables);

        Console.WriteLine(result);
    }

    /// <summary>
    /// Demonstrates semantic function invokes another semantic function.
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_NestedSemanticFunction()
    {
        var kernel = GetKernel();

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins/SamplePlugin");

        // Import the OrchestratorPlugin from the plugins directory.
        var semanticFunctions = kernel.ImportPluginFromPromptDirectory(pluginsDirectory, "SamplePlugIn");

        var args = new KernelArguments
        {
            ["input"] = paperAbstract,
            ["language"] = "latin"
        };

        // Get the GetIntent function from the OrchestratorPlugin and run it
        var result = await kernel.InvokeAsync(semanticFunctions["SimplifyAbstractWithParamsAndTranslate"], args);

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

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins/SamplePlugin");

        // Import the OrchestratorPlugin from the plugins directory.
        var semanticFunctions = kernel.ImportPluginFromPromptDirectory(pluginsDirectory, "SamplePlugIn");

        var functions = kernel.CreatePluginFromObject(new SamplePlugIn());

        var variables = new KernelArguments
        {
            ["input"] = "Execute the function exponent with following arguments 42 and 7",
        };

        // Get the GetIntent function from the OrchestratorPlugin and run it
        var result = await kernel.InvokeAsync<string>(semanticFunctions["MathOperationExtractor"], variables);

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

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins/SamplePlugin");

        var nativeFunctions = kernel.CreatePluginFromObject(new SamplePlugIn(kernel));

        var variables = new KernelArguments
        {
            ["input"] = "Execute the function exponent with following arguments 2 and 16",
        };

        // Get the GetIntent function from the OrchestratorPlugin and run it
        var result = await kernel.InvokeAsync(nativeFunctions["ExecuteMathOperationFunction"], variables);

        Console.WriteLine(result);
    }



    /// <summary>
    /// Demonstrates semantic function invokes another native function.
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_SemanticFunctionInvokesNativeFunction()
    {
        var kernel = GetKernel();

        //var functions = kernel.ImportPluginFromObject(new StringSkill())["StringPlugin"];

        var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SemanticPlugins/SamplePlugin");

        // Import the OrchestratorPlugin from the plugins directory.
        var semanticFunctions = kernel.CreatePluginFromPromptDirectory(pluginsDirectory, "SamplePlugIn");

        var variables = new KernelArguments
        {
            ["input"] = paperAbstract,
            ["language"] = "bosnian"
        };

        // Get the GetIntent function from the OrchestratorPlugin and run it
        var result = await kernel.InvokeAsync<string>(semanticFunctions["AbstractWordCounter"], variables);

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
        OpenAIPromptExecutionSettings sett = new OpenAIPromptExecutionSettings()
        { MaxTokens = 100, Temperature = 0.4, TopP = 1, PresencePenalty = 0.0, FrequencyPenalty = 0.0 };


        //var settings = new new PromptTemplateConfig { ModelSettings = new  maxTokens = 500 };
        var myJokeFunction = kernel.CreateFunctionFromPrompt(myJokePrompt, sett);
        var myPoemFunction = kernel.CreateFunctionFromPrompt(myPoemPrompt, sett);
        var myMenuFunction = kernel.CreateFunctionFromPrompt(myMenuPrompt, sett);

        // Get the GetIntent function from the OrchestratorPlugin and run it
        var jokeRes = await kernel.InvokeAsync<string>(myJokeFunction, new() { ["input"] = "Damir Dobric Microsoft Regional Director" });

        Console.WriteLine(jokeRes);

        var poemRes = await kernel.InvokeAsync<string>(myPoemFunction, new() { ["input"] = "Damir Dobric Microsoft Regional Director" });

        Console.WriteLine(poemRes);

        var menuFunc = await kernel.InvokeAsync<string>(myMenuFunction, new() { ["input"] = "Damir Dobric Microsoft Regional Director" });

        Console.WriteLine(menuFunc);
    }



    /// <summary>
    /// Using native skill for grounding.
    /// </summary>
    /// <returns></returns>
    private static async Task Sample_GroundingWithNativeSkill()
    {
        Kernel kernel = GetKernel();

        string skPrompt = @"
        You have a knowledge of international days.

        Is date {{$input}} known for something?
        ";

        kernel.ImportPluginFromObject(new MyDateTimePlugin(), "datetime");

        kernel.ImportPluginFromObject(new SamplePlugIn(), "sample");

        var sematicFunc = kernel.CreateFunctionFromPrompt(skPrompt, new OpenAIPromptExecutionSettings() { MaxTokens = 150, Temperature = 0.4, TopP = 1 }, functionName: "FncName");

        var result = await kernel.InvokeAsync<string>(sematicFunc, new() { ["input"] = DateTime.Now.ToString("MMM/dd")! });

        Console.WriteLine(result);

        skPrompt = @"
        You have a knowledge of international days.

        {{sample.Enrich}}

        Is date {{$input}} known for something? Also provide most useful historical data for at least two popular events at that day.
        ";

        sematicFunc = kernel.CreateFunctionFromPrompt(skPrompt, new OpenAIPromptExecutionSettings() { MaxTokens = 150, Temperature = 0.4, TopP = 1 });

        result = await kernel.InvokeAsync<string>(sematicFunc, new() { ["input"] = DateTime.Now.ToString("MMM/dd")! });

        Console.WriteLine(result);
    }

    private static async Task Sample_StateMachine()
    {
        Kernel kernel = GetKernel();

        string skPrompt = @"
        {{sample.AddValue}}
        {{sample.AddValue}}
        Tell me about the meaning of the number  {{$mystate}}.
      {{$available_functions}}
        ";

        kernel.ImportPluginFromObject(new SamplePlugIn(), "sample");

        var variables = new KernelArguments();
        variables["mystate"] = "running...";

        var semanticFunc = kernel.CreateFunctionFromPrompt(skPrompt, new OpenAIPromptExecutionSettings { MaxTokens = 150 });

        var result = await kernel.InvokeAsync(semanticFunc, variables);

        Console.WriteLine(result);
    }

    private static async Task Sample_SemanticSkills()
    {
        Kernel kernel = GetKernel();

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


        var summarizePlugIn = kernel.ImportPluginFromPromptDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SemanticPlugIns"), "SemanticPlugIns");

        foreach (var plugIn in kernel.Plugins)
        {
            Console.WriteLine("PlugIn: " + plugIn.Name);
        }

        //var functions = kernel.Plugins.Functions.GetFunctionViews();
        ////ConcurrentDictionary<string, List<FunctionView>> nativeFunctions = functions.Where(fnc=>fnc.NativeFunctions;
        ////ConcurrentDictionary<string, List<FunctionView>> semanticFunctions = functions.SemanticFunctions;

        //foreach (var view in functions)
        //{
        //    Console.WriteLine("Skill: " + view.Name);
        //    // foreach (FunctionView func in view.) { PrintFunction(func); }
        //}

        var result = await kernel.InvokeAsync<string>(summarizePlugIn["Summarize"], new() { [""] = prompt1 });

        Console.WriteLine(result);
    }


    /// <summary>
    /// Demonstrates semantic function invokes another native function.
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_StepwisePlaner()
    {
        var kernel = GetKernel();

        var functions = kernel.ImportPluginFromObject(new semantickernelsample.NativePlugIns.MathPlugin(), "MathPlugin");

#pragma warning disable SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var options = new FunctionCallingStepwisePlannerOptions
        {
            MaxIterations = 15,
            MaxTokens = 4000,
        };

        var planner = new FunctionCallingStepwisePlanner(options);

        //var planner = new SequentialPlanner(kernel);

        var ask = "If my investment of 2130.23 dollars increased by 23%, how much would I have after I spent $5 on a latte?";
        //var ask = "Wenn meine Investition von 2130,23 Dollar um 23% gestiegen ist, wie viel hätte ich, nachdem ich 5 Dollar für einen Latte ausgegeben habe?";
        //var ask = "Calculate the sum of numbers, 1,2,3,4,5,6,7 and then divide it by number of elements in the list.";
        //var ask = "Calculate the energy of the 10kg ball falling from sky at the moment of hitting the surface, if tha ball started at 10km height with the start speed of 100m/s.";
        //var ask = "Calculates the quadrat of the sum of first 10 numbers.";

        //var plan = await planner.CreatePlanAsync(ask);

        Console.WriteLine("Plan:\n");
        Console.WriteLine(JsonSerializer.Serialize(planner, new JsonSerializerOptions { WriteIndented = true }));

        // Execute the plan
        var result = await planner.ExecuteAsync(kernel, ask);

        Console.WriteLine("Plan results:");
        Console.WriteLine(result.FinalAnswer.Trim());

        Console.WriteLine($"Chat history:\n{System.Text.Json.JsonSerializer.Serialize(result.ChatHistory)}");

#pragma warning restore SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    }

    public static async Task TemplateExample()
    {
        var kernel = GetKernel();

        // Create a Semantic Kernel template for chat
        var chat = kernel.CreateFunctionFromPrompt(
            @"{{$history}}
            User: {{$request}}
            Assistant: ");

        // Create choices
        List<string> choices = new List<string>() { "ContinueConversation", "EndConversation" };

        // Create few-shot examples
        List<ChatHistory> fewShotExamples = new List<ChatHistory>()
        {
            new ChatHistory
            {
                new ChatMessageContent(AuthorRole.User, "Can you send a very quick approval to the marketing team?"),
                new ChatMessageContent(AuthorRole.System, "Intent:"),
                new ChatMessageContent(AuthorRole.Assistant, "ContinueConversation")
            },
            new ChatHistory{
                new ChatMessageContent(AuthorRole.User, "Thanks, I'm done for now"),
                new ChatMessageContent(AuthorRole.System, "Intent:"),
                new ChatMessageContent(AuthorRole.Assistant, "EndConversation")
        }
        };

        // Create handlebars template for intent
        var getIntent = kernel.CreateFunctionFromPrompt(
            new()
            {
                Template = """
                           <message role="system">Instructions: What is the intent of this request?
                           Do not explain the reasoning, just reply back with the intent. If you are unsure, reply with {{choices[0]}}.
                           Choices: {{choices}}.</message>

                           {{#each fewShotExamples}}
                               {{#each this}}
                                   <message role="{{role}}">{{content}}</message>
                               {{/each}}
                           {{/each}}

                           {{#each chatHistory}}
                               <message role="{{role}}">{{content}}</message>
                           {{/each}}

                           <message role="user">{{request}}</message>
                           <message role="system">Intent:</message>
                           """,
                TemplateFormat = "handlebars"
            },
            new HandlebarsPromptTemplateFactory()
        );

        ChatHistory history = [];

        // Start the chat loop
        while (true)
        {
            // Get user input
            Console.WriteLine("User > ");
            var request = Console.ReadLine();

            // Invoke prompt
            var intent = await kernel.InvokeAsync(
                getIntent,
                new()
                {
                    { "request", request },
                    { "choices", choices },
                    { "history", history },
                    { "fewShotExamples", fewShotExamples }
                }
            );

            // End the chat if the intent is "Stop"
            if (intent.ToString() == "EndConversation")
            {
                break;
            }

            // Get chat response
            var chatResult = kernel.InvokeStreamingAsync<StreamingChatMessageContent>(
                chat,
                new()
                {
                    { "request", request },
                    { "history", string.Join("\n", history.Select(x => x.Role + ": " + x.Content)) }
                }
            );

            // Stream the response
            string message = "";
            await foreach (var chunk in chatResult)
            {
                if (chunk.Role.HasValue)
                {
                    Console.WriteLine(chunk.Role + " > ");
                }

                message += chunk;
                Console.WriteLine(chunk);
            }
            Console.WriteLine();

            // Append to history
            history.AddUserMessage(request!);
            history.AddAssistantMessage(message);
        }

    }


    /// <summary>
    /// Demonstrates semantic function invokes another native function.
    /// </summary>
    /// <returns></returns>
    public static async Task Sample_FictionPlaner()
    {
        var kernel = GetKernel();

        var sampleFunctions = kernel.ImportPluginFromObject(new SamplePlugIn(), "SamplePlugin");

#pragma warning disable SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var options = new FunctionCallingStepwisePlannerOptions
        {
            MaxIterations = 15,
            MaxTokens = 4000,
        };
        var planner = new FunctionCallingStepwisePlanner(options);

        var ask = "Please calculate the fiction between the stone and alpha centaury with the contraction jumping of 150 kobasica.";

        //var plan = await planner.CreatePlanAsync(ask);

        Console.WriteLine("Plan:\n");
        Console.WriteLine(JsonSerializer.Serialize(planner, new JsonSerializerOptions { WriteIndented = true }));

        // Execute the plan
        var result = await planner.ExecuteAsync(kernel, ask);

        Console.WriteLine("Plan results:");
        Console.WriteLine(result.FinalAnswer!.Trim());

#pragma warning restore SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


    }

    public static async Task Sample_FictionWithFunctionCall()
    {
        var kernel = GetKernel();

        var plugIn = kernel.ImportPluginFromObject(new SamplePlugIn(), "SamplePlugin");

        // Create chat history
        var history = new ChatHistory();

        // Get chat completion service
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var ask = "Please calculate the fiction between the stone and alpha centaury with the contraction jumping of 150 sausages.";

        // Add user input
        history.AddUserMessage(ask);

        // Enable auto function calling
        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        // Get the response from the AI
        var result = await chatCompletionService.GetChatMessageContentAsync(
            history,
            executionSettings: openAIPromptExecutionSettings,
            kernel: kernel);

        // Print the results
        Console.WriteLine("Assistant: " + result);
    }

    public static async Task ES_BookHours()
    {
        var kernel = GetKernel();

        var sampleFunctions = kernel.ImportPluginFromObject(new SamplePlugIn(), "SamplePlugin");

#pragma warning disable SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var options = new FunctionCallingStepwisePlannerOptions
        {
            MaxIterations = 15,
            MaxTokens = 4000,
        };
        var planner = new FunctionCallingStepwisePlanner(options);


        var ask = "Please book 2 hours in Employee Service at on the project 'Contoso project ALPHA' 25.March.";

        //var plan = await planner.CreatePlanAsync(ask);

        Console.WriteLine("Plan:\n");
        Console.WriteLine(JsonSerializer.Serialize(planner, new JsonSerializerOptions { WriteIndented = true }));

        // Execute the plan
        var result = await planner.ExecuteAsync(kernel, ask);

        Console.WriteLine("Plan results:");
        Console.WriteLine(result.FinalAnswer!.Trim());

#pragma warning restore SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    }

    private static void WorkingWithTokens()
    {
        var encoding = Tiktoken.Encoding.ForModel("gpt-4");
        var tokens = encoding.Encode("hello world"); // [15339, 1917]
        var text = encoding.Decode(tokens); // hello world
        var numberOfTokens = encoding.CountTokens(text); // 2
        var stringTokens = encoding.Explore(text); // ["hello", " world"]

        // Go to tokenizer and try it: https://platform.openai.com/tokenizer
        tokens = encoding.Encode("Guten Tag aus Nürnberg"); // [15339, 1917]
        text = encoding.Decode(tokens); // hello world 8
        numberOfTokens = encoding.CountTokens(text); // 
        stringTokens = encoding.Explore(text);


        var encoding2 = Tiktoken.Encoding.Get(Encodings.P50KBase);
        var tokens2 = encoding.Encode("hello world"); // [31373, 995]
        var text2 = encoding.Decode(tokens); // hello world
    }

    #region Helpers

    //private static void PrintFunction(FunctionView func)
    //{
    //    Console.WriteLine($"   {func.Name}: {func.Description}");

    //    if (func.Parameters.Count > 0)
    //    {
    //        Console.WriteLine("      Params:");
    //        foreach (var p in func.Parameters)
    //        {
    //            Console.WriteLine($"      - {p.Name}: {p.Description}");
    //            Console.WriteLine($"        default: '{p.DefaultValue}'");
    //        }
    //    }

    //    Console.WriteLine();
    //}


    private static Kernel GetKernel()
    {
       //return GetOpenAIKernel();
        return GetAzureKernel();
    }


    private static Kernel GetOpenAIKernel(string? useChatOrTextCompletionModel = "chat")
    {
        Kernel kernel;

        if (useChatOrTextCompletionModel == null || useChatOrTextCompletionModel == "chat")
        {
            kernel = Kernel.CreateBuilder()
             .AddOpenAIChatCompletion(
            Environment.GetEnvironmentVariable("OPENAI_CHATCOMPLETION_DEPLOYMENT")!, // The name of your deployment (e.g., "gpt-3.5-turbo")
            Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
            Environment.GetEnvironmentVariable("OPENAI_ORGID")!)
        .Build();
        }
        else
            throw new Exception("Text Completion Models are deprected.");

        return kernel;
    }

    private static Kernel GetAzureKernel(string? useChatOrTextCompletionModel = "chat")
    {
        Kernel kernel;

        if (useChatOrTextCompletionModel == null || useChatOrTextCompletionModel == "chat")
        {
            kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                Environment.GetEnvironmentVariable("AZURE_OPENAI_CHATCOMPLETION_DEPLOYMENT")!,  // The name of your deployment (e.g., "text-davinci-003")
                Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!,    // The endpoint of your Azure OpenAI service
                Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!      // The API key of your Azure OpenAI service
            )
            .Build();
        }
        else
        {
            kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
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

