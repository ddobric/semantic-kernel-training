using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Tiktoken;

namespace semantickernelsample
{
    /// <summary>
    /// Demonstrates how to chunk text.
    /// </summary>
    internal class RagSample
    {
        private Kernel _kernel;

        class MyInMemoryVector
        {
            public string Ref { get; set; }

            public float[] Embedding { get; set; }

            public string Chunk { get; set; }
        }


        public RagSample(Kernel kernel)
        {
            _kernel = kernel;
        }

        private const string _text = """
        The city of Venice, located in the northeastern part of Italy,
        is renowned for its unique geographical features. Built on more than 100 small islands in a lagoon in the
        Adriatic Sea, it has no roads, just canals including the Grand Canal thoroughfare lined with Renaissance and
        Gothic palaces. The central square, Piazza San Marco, contains St. Mark's Basilica, which is tiled with Byzantine
        mosaics, and the Campanile bell tower offering views of the city's red roofs.

        The Amazon Rainforest, also known as Amazonia, is a moist broadleaf tropical rainforest in the Amazon biome that
        covers most of the Amazon basin of South America. This basin encompasses 7 million square kilometers, of which
        5.5 million square kilometers are covered by the rainforest. This region includes territory belonging to nine nations
        and 3.4 million square kilometers of uncontacted tribes. The Amazon represents over half of the planet's remaining
        rainforests and comprises the largest and most biodiverse tract of tropical rainforest in the world.

        The Great Barrier Reef is the world's largest coral reef system composed of over 2,900 individual reefs and 900 islands
        stretching for over 2,300 kilometers over an area of approximately 344,400 square kilometers. The reef is located in the
        Coral Sea, off the coast of Queensland, Australia. The Great Barrier Reef can be seen from outer space and is the world's
        biggest single structure made by living organisms. This reef structure is composed of and built by billions of tiny organisms,
        known as coral polyps.

        Damir Dobric is a dancing teacher from frankfurt am main. Regularly dancing on tech-stages.
        
        """;

        private string _experimentText = """
        sales account: priesner philip
        segmentierung: small, mid
        portfolio_kategorie: network and security, networking, wlan
        integration: wahr
        mandant: acp oberösterreich
        leistungserbringer: leingartner christoph
        kaufmännische leistungsbeschreibung: leistungsbeschreibung
        hersteller: aruba
        kurzbeschreibung: the aruba mobility master is the next generation of master controller that can be either deployed as a virtual machine (vm) or installed on an x86-based hardware appliance. the mobility master provides better user experience, flexible deployment, simplified operations and enhanced performance. existing aruba customers can migrate their master controller configuration and licenses over to the mobility master and start taking advantage of these unique capabilities.
        branche: branchenunabhängig
        leistungsart: lokale leistung
        technische leistungsbeschreibung: siehe kaufmännische leistungsbeschreibung
        inhaltstyp: produkt
        """;

        private string _experimentTextReturnedByApplicaiton = """
        sales account: turk-gabel christoph, mikl thomas
        portfolio_kategorie: modern workplace, workplace devices​, computing devices
        integration: falsch
        mandant: acp süd
        leistungserbringer: proprentner hans peter
        support: falsch
        kaufmännische leistungsbeschreibung: ​​das clientmanagement windows stellt den betrieb und das management eines windows client inklusive windows-patchmanagement ohne jeglicher applikation oder ähnliches zur verfügung.
        managed: wahr
        hersteller: acp
        kurzbeschreibung: ​​das clientmanagement windows stellt den betrieb und das management eines windows client inklusive windows-patchmanagement ohne jeglicher applikation oder ähnliches zur verfügung
        leistungsart: lokale leistung
        inhaltstyp: service
        consulting: falsch
        verrechnungseinheit: pro client
        kostenart: laufende kosten
        minimaler_vk: 42
        artikelnummer: man-client-w
        empfohlener_vk: 48
        development: falsch
        """;

        //Damir Dobric is a dancing teacher from frankfurt am main. Regularly dancing on tech-stages.
        private const string _experimentText2 = """
            
            Following text contains information you must analyze to generate the proper answer. 
            Information is grouped into multiple sources. 
            Your answer should be taken from all listed sources and well formatted. 
            If the data in sources is not related to the user's intent, say to user, that you cannot find the data.
            Answer from publicly trainied data.
            If the text contains any Reference (like URL), put the URL at the end of the answer. For example: Reference: https://daenet.de/abc.pdf 
            The Reference should be printed as it is, without any modifications, and should not be duplicated or invalidated.
            ----- BEGIN Source 0 ------
            INYOUSE


            Following is the URL, Reference or Source:
            Reference: [https://acpcloud.sharepoint.com/sites/intranet-adh-portfolio/SitePages/inyouse.aspx](https://acpcloud.sharepoint.com/sites/intranet-adh-portfolio/SitePages/inyouse.aspx)


            sales account:
                kreis torsten
              segmentierung:
                mid, small, large
              portfolio_kategorie:
                digital solutions, smarte industrie, smartes arbeiten
              integration:
                true
              mandant:
                acp oberösterreich
              leistungserbringer:
                lex-nikohl silke
              support:
                true
              kaufmännische leistungsbeschreibung:
                 it's an easy all in one solutiondurch die möglichkeiten von extended reality und holografie schafft inclusify inyouseeine völlig neue form der zusammenarbeit durch die symbiose aus​​​​​​​menschen, informationen und der umgebung. ar remote solutioneffizienter ressourceneinsatztechniker können störungen oder eine wartung durch unterstützung aus der ferne durchführen. dadurch steigt deine first-time-fix-rate und du sparst reisekosten. ganz nebenbei gewinnt dein unternehmen jede menge zeit um sich anderen herausforderungen zu widmen.wissenstransferdurch die konservierung von expertenwissen in form von augmented-reality gestützten anleitungen ermöglichst du eine 24/7 bereitstellung von wichtigen informationen.neue nachhaltigkeiteine nicht notwendige reise ist eine gute reise. durch die befähigung von personen vor ort, sparst du unnötige reisen und optimierst eure co2-bilanz.vorsprung im wettbewerbhohe reaktionszeiten und ein moderner arbeitsplatz stärken euer unternehmen und die positive ausstrahlung auf kundschaft sowie als arbeitgebermarke. collaborationend to endinclusify inyouse bringt alles mit, was für die verbindung zwischen technikern, kunden und supportbenötigt wird. einfach auspacken, einschalten – funktioniert in minuten.perfekt angepasster und einfacher digitaler serviceprozess für die vermittlung zwischen techniker und operatorskalierbare videotelefonie mit niedriger latenzzeitbestehende systeme lassen sich über unsere lösung unkompliziert anbindenkeine systementscheidung wie microsoft teams oder cisco webex nötig du möchtest mehr wissen - hier findest du einentrailer oder kannst eine live demo für deinen kunden buchen 
              managed:
                true
              hersteller:
                acp
              kurzbeschreibung:
                inclusify inyouse ist eine remote service plattform mit augmented reality/ extended reality. technisches fachpersonal bekommt so visuelle unterstützung beim lösen von problemen im arbeitsalltag.
              branche:
                branchenunabhängig
              leistungsart:
                leistung einer kompetenz gesellschaft
              technische leistungsbeschreibung:
                keywords im kontext des eintrags:empowering people, content studio, inklusion, ar, augmented reality, digital, digital gesellschaft, 
              inhaltstyp:
                produkt
              consulting:
                true
              development:
                true
            Properties:
            Score:0,5302236676216125



            ----- END Source   0 -------
            ----- BEGIN Source 1 ------
            Workato - Datendrehscheibe mit 1000+ Schnittstellen as a Service


            Following is the URL, Reference or Source:
            Reference: [https://acpcloud.sharepoint.com/sites/intranet-adh-portfolio/SitePages/workato-datendrehscheibemit1000schnittstellenasaservice.aspx](https://acpcloud.sharepoint.com/sites/intranet-adh-portfolio/SitePages/workato-datendrehscheibemit1000schnittstellenasaservice.aspx)


            sales account:
                kreis torsten
              portfolio_kategorie:
                digital solutions, smarte daten
              integration:
                true
              mandant:
                acp oberösterreich
              leistungserbringer:
                lex-nikohl silke
              support:
                true
              managed:
                false
              hersteller:
                acp
              kurzbeschreibung:
                integration-as-a-platform ipaas ist noch ein junger aber sich durchsetzender bereich der cloud-integration und -automation. im weltweit führenden it-markt usa ist das um standard geworden. workato ist weltweit führend im reifegrad der bereits fertigen konnektoren. es können einfach und schnell onpremise- und cloud-systeme beliebig integriert und damit verbunden werden. moderne it-architekturen erfordern diese vorgehensweise.
              leistungsart:
                leistung einer kompetenz gesellschaft
              technische leistungsbeschreibung:
                 hier folgt ihre technische leistungsbeschreibunglorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. at vero eos et accusam et justo duo dolores et ea rebum. stet clita kasd gubergren, no sea takimata sanctus est lorem ipsum dolor sit amet. lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. at vero eos et accusam et justo duo dolores et ea rebum. stet clita kasd gubergren, no sea takimata sanctus est lorem ipsum dolor sit amet. lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. at vero eos et accusam et justo duo dolores et ea rebum. stet clita kasd gubergren, no sea takimata sanctus est lorem ipsum dolor sit amet. duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis at vero eros et accumsan et iusto odio dignissim qui blandit praesent luptatum zzril delenit augue duis dolore te feugait nulla facilisi. lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod tincidunt ut laoreet dolore magna aliquam erat volutpat.  
              inhaltstyp:
                produkt
              consulting:
                true
              development:
                true
            Properties:
            Score:0,5063320398330688



            ----- END Source   1 -------
            ----- BEGIN Source 2 ------
            Learn XR-Workshop


            Following is the URL, Reference or Source:
            Reference: [https://acpcloud.sharepoint.com/sites/intranet-adh-portfolio/SitePages/learnxr-workshop.aspx](https://acpcloud.sharepoint.com/sites/intranet-adh-portfolio/SitePages/learnxr-workshop.aspx)


            sales account:
                kreis torsten
              portfolio_kategorie:
                digital solutions, smartes leben, smarte beratung, smarte industrie, smartes arbeiten, smarte daten
              integration:
                false
              mandant:
                acp oberösterreich
              leistungserbringer:
                lex-nikohl silke
              support:
                true
              kaufmännische leistungsbeschreibung:
                 unser ansatzvon der beratung über das coaching und die umsetzung bis hin zum training:unsere expertenanalysieren deine herausforderungen,entwickeln maßgeschneiderte lösungspakete undbegleiten dich bei der umsetzung in die praxis.so nutzt du kundenorientiert und zielführend das volle potenzial digitaler technologien für dein unternehmen. unser 3-phasen-modell für erfolgreiche digitale projektedas 3-phasen-modell von inclusify ist die essenz unseres denkens und handelns. es ist unsere dna und vereint unseren anspruch, dein team für veränderungen zu begeistern und es diese veränderungen selbst gestalten zu lassen. denn technologien sind nur der anfang. damit du sie erfolgreich nutzen kannst, müssen du und dein team an einem strang ziehen, den mehrwert der digitalisierung verstehen und das handling der neuen tools verinnerlichen. das geht nicht von heute auf morgen. echter wandel ist kein sprint, sondern ein marathon.unser 3-phasen-modell begleitet euch deshalb schritt für schritt auf dem weg zum ziel.   schritt 1: technische sensibilisierung und methodische begleitungherausforderungen verstehenlösungen definierenuse-cases betrachtenerfahre, was die einzelnen technologien für dein unternehmen leisten können und wie du mit ihnen selbst hochkomplexe herausforderungen meisterst    schritt 2:erhebung und modellierungtechnologien anwenden, erhebung des status quoanalyse der rahmenbedingungenzieldefinitionprototypen entwickeln und testenvom proof of concept zum proof of value – gemeinsam erarbeiten wir konkrete anwendungsfälle und definieren, welche technologien für dein unternehmen infrage kommen. anschließend gehen wir in die technische umsetzung, bauen prototypen und prüfen, ob diese den erwartungen deiner kunden entsprechen. so finden wir gemeinsam heraus, was technisch machbar und sinnvoll ist.    schritt 3:roll-out und trainingintensive trainingsintegration vornehmentechnologien verankernin intensiven trainings coachen wir dein team im umgang mit den verschiedenen technologien und sorgen so für echte akzeptanz über alle hierarchieebenen hinweg.    hier findest du unseren flyer, den du bei bedarf downloaden kannst  unsere referenzen und weitere details auf unserer webseite 
              managed:
                false
              hersteller:
                acp
              kurzbeschreibung:
                von der beratung über das coaching und die umsetzung bis hin zum training: unsere experten analysieren deine herausforderungen, entwickeln maßgeschneiderte lösungspakete und begleiten dich bei der umsetzung in die praxis.
              leistungsart:
                leistung einer kompetenz gesellschaft
              technische leistungsbeschreibung:
                keywords im kontext des eintrags:ar, mr, vr, transformation
              inhaltstyp:
                produkt
              consulting:
                true
              development:
                true
            Properties:
            Score:0,4959818422794342



            ----- END Source   2 -------
            ----- BEGIN Source 3 ------
            Kundenspezifische Softwareentwicklung Cloud Lösungen


            Following is the URL, Reference or Source:
            Reference: [https://acpcloud.sharepoint.com/sites/intranet-dae-portfolio/SitePages/kundenspezifischesoftwareentwicklungcloudlsungen.aspx](https://acpcloud.sharepoint.com/sites/intranet-dae-portfolio/SitePages/kundenspezifischesoftwareentwicklungcloudlsungen.aspx)


            sales account:
                aevermann stefan
              portfolio_kategorie:
                digital solutions, smarte industrie, smarte beratung
              integration:
                true
              mandant:
                acp oberösterreich
              leistungserbringer:
                dobric damir
              support:
                true
              kaufmännische leistungsbeschreibung:
                   großartige ideen, aber keine lösung oder begrenzte ressourcen oder fähigkeitenkein problem.euer kunde bringt das geschäftliches know-how und seine ideen ein.wir als daenet stellen ein team von 1-5 personen mit den fähigkeiten und kompetenzen zusammen,das den anforderungen am besten entspricht!unsere vorteile:wir arbeiten mit modernen, agilen technologien, die eine lange, nachhaltige lebensdauer der neuen anwendungen sicherstellen.wir stehen für eine vertrauensvolle partnerschaft, die eine kontinuierliche verbesserung der lösung garantiert.auch die jahrelange, fundierte erfahrung in der remote-entwicklung spricht für uns. unser angebot:1. envisioning workshopziel des workshops ist es, die konkrete problemstellung in 1-2 tagen zu verstehen und die lösung zu formulieren/entwickeln.2. solution assessment/sowgemeinsam mit den business experten des kunden erstellen wir den projektumfang (sow = statement of work), indem wir die anforderungen beschreiben, die die endgültige lösung definieren.wir bieten die notwendige transparenz, um die entsprechenden einblicke in die geschäftlichen auswirkungen, die erwarteten kosten und die technologischen vorteile der anwendungsentwicklung zu erhalten.3. implementierungabhängig von den anforderungen stellen wir für euren kunden ein team zusammen, das seinen anforderungen optimal entspricht, um auch einen zügigen projektstart und eine hohe kosteneffizienz zu gewährleisten.4. betrieb und supportsobald die lösung oder der teil der lösung produktiv genutzt werden kann, übernehmen wir die installation, den betrieb und den support.wir sind ein von microsoft autorisierter cloud solution partner und bringen die lösung für eure kunden in die cloud.für alle von uns implementierten lösungen bieten wir cloud-hosting an.     unsere leistungen im internet  
              managed:
                true
              hersteller:
                microsoft
              kurzbeschreibung:
                großartige ideen, aber keine lösung auf dem markt wird den anforderungen an innovations- und wettbewerbsführerschaft gerecht. begrenzte ressourcen oder fähigkeiten für die anstehenden entwicklungsaufgaben im cloud-umfeld
              leistungsart:
                leistung einer kompetenz gesellschaft
              technische leistungsbeschreibung:
                kundenspezifische software-entwicklung, softwareentwicklung in der cloud, engineering nach maß 
              inhaltstyp:
                produkt
              referenz:
                https://daenet.de/de/references/
              consulting:
                true
              development:
                true
            Properties:
            Score:0,4953230321407318



            ----- END Source   3 -------
            ----- BEGIN Source 4 ------
            Digitale Bildung – ACP eduWERK Academy


            Following is the URL, Reference or Source:
            Reference: [https://acpcloud.sharepoint.com/sites/intranet-atr-portfolio/SitePages/digitalebildungacpeduwerkacademy.aspx](https://acpcloud.sharepoint.com/sites/intranet-atr-portfolio/SitePages/digitalebildungacpeduwerkacademy.aspx)


            sales account:
                thalmann sonja
              portfolio_kategorie:
                modern workplace, employee experience and new work design, digital workspace, end-user experience and monitoring, cloud workplace, applications, microsoft 365, uc and collaboration, collaboration and communication
              integration:
                false
              mandant:
                acp oberösterreich
              leistungserbringer:
                thalmann sonja
              support:
                false
              kaufmännische leistungsbeschreibung:
                  was verbirgt sich hinter der acp eduwerk academy?diese wurde mit dem ziel ins leben gerufen, die erfahrungen jener pädagog:innen, die in der praxis mit unseren lösungen arbeiten, von einer schule zur nächsten weiterzugeben.die aufbauenden trainings sind komplett individuell.wir orientieren uns einerseits an der technische umsetzung der acp eduwerk lösungen und andererseits an den bedürfnisse der einzelnen schulekundenindividuell wie unsere lösungen, passen auch wir uns an!  wie sieht unser lösungsansatz aus? was bieten wir als acp eduwerk an?die digitalisierung der schule benötigt zielgerichtete und systematische unterstützung:wie funktioniert "digitale schule"?welche pädagogischen ziele möchten der kunde mit der digitalen schule erreichen?wie können diese ziele erreicht werden?unsere drei zentralen ansatzpunkte:workshopcoachingtraining ein paar details zu unserem lösungsansatz:im rahmen eines einmaligen workshops erstellen wir gemeinsam mit den pädagog:innen ein digitales konzeptsinhalte des workshops:analyse und beurteilung der ausgangssituationkennenlernen wesentlicher elemente der digitalen schule und eines digitalen konzeptserarbeitung von maßgeschneiderten zielen & maßnahmen für ihr digitales konzeptauf die laufende begleitung durch regelmäßiges coaching und training kommt es anwie ist unser vorgehen:besprechung des fortschritts in der umsetzung des digitalen konzepts inkl. evaluierung und anpassung gesetzter maßnahmenmoderation & input hinsichtlich lösungsmöglichkeiten bei problempunktenneuer input zu unterrichtsszenarien mit digitalen endgeräten zu vereinbarten themenschwerpunktenwir haben dein interesse geweckt?weitere informationen findest du auf unserer webseite noch immer nicht genug?​​​​​​​nachstehend findest du weitere interessante links
            

            """;

        public void SplitTextToChunks()
        {
            Console.WriteLine("=== Text chunking with chunk header ===");

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var lines = TextChunker.SplitPlainTextLines(_text, 40);
            var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, 150, chunkHeader: "DocRef: test.txt\n\n");
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            var encoder = ModelToEncoder.For("gpt-4o"); // or explicitly using new Encoder(new O200KBase())

            //
            // Create lines of texts.
            foreach (var line in lines)
            {
                var cnt = encoder.CountTokens(line);
                Console.WriteLine($"{cnt} \t- {line}");
            }

            Console.WriteLine();

            //
            // Create paragraphs from lines of text.
            Console.WriteLine("=== Paragraphs ===");

            foreach (var paragraph in paragraphs)
            {
                var cnt = encoder.CountTokens(paragraph);
                Console.WriteLine($"{cnt} \t- {paragraph}");
            }
        }

        private List<MyInMemoryVector> _memory = new List<MyInMemoryVector>();

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        public async Task RunRAG()
        {
            Console.WriteLine("------------- (1) Chanking -----------");

            var lines = TextChunker.SplitPlainTextLines(_text, 40);
            var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, 50, chunkHeader: "DOCUMENT Ref: test.txt\n\n");

            var encoder = ModelToEncoder.For("gpt-4o"); // or explicitly using new Encoder(new O200KBase())

            Console.WriteLine("------------- (2) Creating Embeddings -----------");

            //var vector = await GetEmbedding(_text);
            //_memory.Add(new MyInMemoryVector
            //{
            //    Chunk = _text,
            //    Ref = "some reference",
            //    Embedding = vector.ToArray(),
            //});

            foreach (var paragraph in paragraphs)
            {
                // Use this if you want to know haw many tokens you have.
                var cnt = encoder.CountTokens(paragraph);

                // Create embedding vector for the text.
                var vector = await GetEmbedding(paragraph);
                _memory.Add(new MyInMemoryVector
                {
                    Chunk = paragraph,
                    Ref = "some reference",
                    Embedding = vector.ToArray(),
                });
            }

            await RunConversationLoopAsync();
        }

        public async Task RunSemanticExperiment()
        {

        }
        protected async Task RunSemanticExperiment(string prompt, int chunkTokenSize)
        {
            Console.WriteLine("------------- (1) Chanking -----------");

            var lines = TextChunker.SplitPlainTextLines(_experimentText, int.MaxValue);
            var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, chunkTokenSize, chunkHeader: String.Empty);

            var encoder = ModelToEncoder.For("gpt-4o"); // or explicitly using new Encoder(new O200KBase())

            Console.WriteLine("------------- (2) Creating Embeddings -----------");

            var vector = await GetEmbedding(_experimentText);
            _memory.Add(new MyInMemoryVector
            {
                Chunk = _text,
                Ref = "some reference",
                Embedding = vector.ToArray(),
            });

            await RunConversationLoopAsync();
        }
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        private async Task<float[]> GetEmbedding(string chunk)
        {
            ITextEmbeddingGenerationService embeddingService = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();

            var embedding = await embeddingService.GenerateEmbeddingAsync(chunk);

            return embedding.ToArray();
        }
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        private async Task RunConversationLoopAsync()
        {
            var history = new ChatHistory();

            // Get chat completion service
            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

            // Start the conversation
            Console.Write("User > ");

            string? userInput;

            history.AddSystemMessage("You are assistent who helps user to find answers.");

            while ((userInput = Console.ReadLine()) != null)
            {
                // Enable auto function calling
                OpenAIPromptExecutionSettings executionSettings = new()
                {
                    Temperature = 0.0,
                    MaxTokens = 1500
                };

                //#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                //                var ollamaSettings = new OllamaPromptExecutionSettings
                //                {
                //                    Temperature = 0.1f,
                //                };
                //#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


                var match = await FindBestMatchAsync(userInput);

                // We add the chunk here
                history.AddUserMessage(match.Chunk);

                // Add user input
                history.AddUserMessage(userInput);

                // Get the response from the AI
                var result = await chatCompletionService.GetChatMessageContentAsync(
                    history,
                    executionSettings: executionSettings,//ollamaSettings,
                    kernel: _kernel);

                // Print the results
                Console.WriteLine("Assistant > " + result);

                // Add the message from the agent to the chat history
                history.AddMessage(result.Role, result.Content ?? string.Empty);

                // Get user input again
                Console.Write("User > ");
            }
        }

        public async Task RunExperimentLoopAsync()
        {
            var chunkSize = 0; // Set your desired chunk size here
            var prompt = "Leingartner Christoph";
            var noiseWord = "acp"; // This word will be added to the text in each iteration

            await RunExperimentLoopAsync(chunkSize, prompt.ToLower(), 
                _experimentText.ToLower()/*.Replace("acp", string.Empty)*/, noiseWord);
        }

        public async Task RunExperimentLoopAsync(int chunkSize, string prompt, string mathingText, string noiseWord)
        {
            var history = new ChatHistory();

            var promptEmbedding = await GetEmbedding(prompt);

            List<double> similarities = new List<double>();

            for (int i = 0; i < 50; i++)
            {
                if (i > 0)
                    mathingText = $"{mathingText} {noiseWord}";

                similarities.Add(CalculateSimilarity(promptEmbedding, await GetEmbedding(mathingText)));
            }

            PrintExperimentResult(chunkSize, similarities);
        }


        protected void PrintExperimentResult(int chunkSize, List<double> similarities)
        {
            using (StreamWriter writer = new StreamWriter($"./ExperimentResults/experiment_{chunkSize}.txt"))
            {
                writer.WriteLine("Experiment results for chunk size: " + chunkSize);
                writer.WriteLine("-------------------------------------------------");
                writer.WriteLine("Similarity scores:");

                foreach (var similarity in similarities)
                {
                    writer.WriteLine(similarity);
                }
            }
        }

        private async Task<MyInMemoryVector> FindBestMatchAsync(string userInput)
        {
            MyInMemoryVector bestMatch = null;

            double best = double.MinValue;

            var userEmbedding = await GetEmbedding(userInput);

            foreach (var entry in _memory)
            {
                var similarity = CalculateSimilarity(userEmbedding, entry.Embedding);

                if (similarity > best)
                {
                    best = similarity;
                    bestMatch = entry;
                }
            }

            return bestMatch!;
        }

        /// <summary>
        /// Calculates the cosine similarity.
        /// </summary>
        /// <param name="embedding1"></param>
        /// <param name="embedding2"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static double CalculateSimilarity(float[] embedding1, float[] embedding2)
        {
            if (embedding1.Length != embedding2.Length)
            {
                return 0;
                //throw new ArgumentException("embedding must have the same length.");
            }

            double dotProduct = 0.0;
            double magnitude1 = 0.0;
            double magnitude2 = 0.0;

            for (int i = 0; i < embedding1.Length; i++)
            {
                dotProduct += embedding1[i] * embedding2[i];
                magnitude1 += Math.Pow(embedding1[i], 2);
                magnitude2 += Math.Pow(embedding2[i], 2);
            }

            magnitude1 = Math.Sqrt(magnitude1);
            magnitude2 = Math.Sqrt(magnitude2);

            if (magnitude1 == 0.0 || magnitude2 == 0.0)
            {
                throw new ArgumentException("embedding must not have zero magnitude.");
            }

            double cosineSimilarity = dotProduct / (magnitude1 * magnitude2);

            return cosineSimilarity;
        }

    }

}

