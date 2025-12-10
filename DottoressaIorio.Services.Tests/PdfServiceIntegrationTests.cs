using DottoressaIorio.Models;
using DottoressaIorio.Services.Pdf;
using Xunit.Abstractions;

namespace DottoressaIorio.Services.Tests;

public class PdfServiceIntegrationTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task GenerateRealPdf_ShouldCreatePdfFile()
    {
        // Arrange
        var builder = new PdfHtmlBuilder();
        var service = new PdfService(builder);

        var therapy = new Therapy
        {
            Patient = new Patient
            {
                Title = "Dr.",
                FirstName = "John",
                LastName = "Doe"
            },
            CreatedDate = new DateTime(2024, 1, 1),
            Description = @"Chi bentornato profondata uno crepitando hai villanella convertira eguagliare. Piramide poi ben per dubitare adorarti una scolpire finestra. Turbare informi ansieta noi viaggio disceso tue suo puo. Dirtelo sveglia sorriso se tornato so porpora le le dovesti. Mi in bell sete egli tu ha sera. Traversa promessa veemenza da in sa. Si animatore ricordate ai va sconvolge rinnovati contenuti. Anch so agli tebe su la io tuoi sono. Afa mio aspettavo indugiare sul ornamento. Su ha dodici sacchi doveva.

Intere chiave mia mostro ben saluti fedele mirato. Uso ben perpetuato una trasognato noi crepitando resistenza. Lo ah me quando se specie tavola gonfio meglio. Sai dubitare speranza cio presenta escirgli qui sul alterata. Modo com ride piu vive non bell. Ora versi dallo ore coppa dev per. Sia appare impero monaco quanto del.

Opaco umana forma amore san afa sta messa gorgo sue. Angolo visite sentii abisso il visita pisana me lo. Il affilate el di scioglie serbatoi ah. Per nobile tue debole quarta afa che. Lunghe fronte stanco dorata era voluto mie dicevi ero dei. Da altissimo cresciuto baciocchi le ah taciturna.

Vi espressiva finalmente io conservava immutabile baciandola. Rimasta ora pel confusa perisce. Esplorarne accostarmi raccontava ve su. So ricordarti avvelenate di restituire disconosci va attitudine. Vedi una oggi eri per cosi lana. Pietro appare ah or soglia. So amuleto antichi le mi inganna officio mi lorenzo miniato.

Re da accento no abbiano ti intorno. Qua medesimo creature arrivato ginocchi vai. San comunicare trascinano era tre troveresti. Vi lacerato da profonda esercita pensiero te da. Estasi armata hai dio sappia dal smorta. Giudichera va bellissima re ed calpestare conosciuto appartiene toglieremo. Udi guardavamo confusione aspettando dolcemente attenzione sia. Mio vai poi caduco quelle stesso mummia. Vecchio abolito se tornano porpora mi. Piccolo ascolto carrara sei maestro aspetto noi tebaide ove.

Spasimo istinto aggrava so fu spirito intendi ti. Luce so riso sera cure temi ci. Verranno sofferto violento non che sembravi trapassi. Ti in persiano montagne ed illumina. Tu bocca quale avete sorte se arabi ve pochi. Su so pensi si ho fiera bella sopra cerca. Com far viso ardi una sono.

Inutile vacilla no ai facendo periodo. Inondi ne or io si poteva posata goccia faremo. Calpestare visitatore no obbedivamo ci cancellato volgendosi ah me difenderlo. Dolore venire si te avviva pieghi ciglia. Mirti abito misto ve della da prima ha. Seguivo non voi per ascolto secondo attende via. Veda da pure rete il dare tu care mese ch. Cercando mia bellezza turbarlo all.

Dante esser tal colui bocca sii getti dagli poi sai. Nervi ti dovra sarei vedro un me pensi. Bearmi ben smorta dir saremo avanza sei membra. Aver tair il il acre po. Ex se lo tornato placato rimasta me pensato. Riprendera fu accompagno ai arrossendo meraviglia so finalmente immobilita. Animo ora certi mai torno. Plasmata sospenda fa stagione tendendo continua or. Ed da attenzione si no volgendosi masticando guarderemo.

Dolce ed andro corse ah farmi ma. Udissi dianti gia voi via sta blocco. Lei ama incontra standole consunta sfolgora. Dovessi vivente aridita di se ch imagina me cattivo conduce. Piena puo chi siete lucca. Stai oh sole ah lo ne onde. Misteriosa declinante ho di sbigottita. Dio camminato via sollecito illusione distrutta riapparve.

Sensitive tue melagrani sollecito dir vicinanza nel parentela. Ami gaddi bel stato certe chi scuse. Pensiero amo risoluto sai illumina chi sventura. Vi siete amate io certo preme. Purezza altezza il sguardo ai precisa venirmi. Dovresti ed ricevute un glorioso.

Brucia vorrei ora talune par pieghi fianco afa. Mettendosi guardatelo trasalendo tua cominciata scomparire espressiva pel. Portali peccato piccolo te fu assouan tu piacque. Mentre sicuro fedele avessi oscure ape guarda dov. Macchia persone bisogna viaggio parlato ben dai ape oro. Sta caro voce col mise ami. Svelto temete chiaro per ape aurora sabbia aprile tre. Immune san brucia pisana aspiro sforzo salivo sta. Vi contiene fu oscurato ai ha tremante giornata.

Solitario ergendosi riapparve rifiutare veramente mie san hai pel. Chi uso potremo pesante all dovette una vivessi. Affonda giu giovini fiumana dal sorrisi secondo dovessi pel sul. Anime scena certo diede voi tanto neghi tua. Ossuta ex dovrei pagine vedere vedrai oscure le. Titubante puo conquista scoprirvi serravano accomiata ora aspettera. Armario deserto secondo me fallano lo insieme pulsare.

Ti ex sollecito da disegnato torturare fu disperata bordatino. In chirurgo spiaggia dovevamo gettando ex so tremante. Ali del nella verro osi amare sogni tende. Vite ambe sola ore mia pago. Bagnava uccelli potermi in sguardo re stanchi offrono le ha. Dov disegnata curiosita sconvolta mio rarissima affannosa cristallo. Hanno morra altre sopra mio vai magra lente. Indebolite dai villanella raccontava noi accecarono dolcemente amo. Una trascinano comunicare una accorrendo pie conoscermi lei.

Incontrati ammirabile tu ah ve crepitando se profondata. La ma rivedro strette so abolito la cattivo parlato. Confronto le si ricordate su so pronunzia. Meno moto io duro se buio. Disconosci indicibile no in implorando ha dormissero irrequieto. Fai mia venirmi resiste ottobre viluppo. Chi sui attesa nemica vedrai una.

Opaco umana forma amore san afa sta messa gorgo sue. Angolo visite sentii abisso il visita pisana me lo. Il affilate el di scioglie serbatoi ah. Per nobile tue debole quarta afa che. Lunghe fronte stanco dorata era voluto mie dicevi ero dei. Da altissimo cresciuto baciocchi le ah taciturna.

Cercarla bastanza tempesta estraneo par com ricevuto montagne. Speranza invocato sfolgora pel violenza chi pie dal. Po ch chiave seduto trarre ex mutare te questa. Di lo cerchi so valere vorrei salire medico voluto. Inatteso trovarti guardato che ricevute poi sembravi. Scolpisco era tua voi baciocchi brillanti pel. Chiamando implorare dal potessero sue sorrideva melagrani con.

Agili per sotto monte fra assai. Ascoltami io splendido affatichi usignuoli so aspettera da su. Tue alghe fiato neghi qua. Del scarabei scoperto cercando prodigio nel angoscia cui rovescio. Illusione disperata sorrideva ch riconobbe su. Dovevamo cio dei entrambi tendendo the tremolio. Feconda il tendono se risolsi. The vergogna desidera turbarlo una paradiso traversa ape sue. Suo vai sospettoso incontrati dio bruciavano gia.

Migliore trapassi ora via pel incendio infinita ambascia. Col resiste bisogno ben andiate piu fascino. Acerbita duchessa smarrito san guardare mettermi sue. Disperato infantile approdare si el il statuette ascoltami scacciare. Fu ah apparenze scolpisca splendido distrutta sollevera curiosita. Tebaide trovera ho al chinava confido tornano. Eroico bel par dentro vedere. Disperato di mi fu davanzale singolare. Dal oro faro far agli vuoi. Or seguivo ho andiate trasale intatta imagine intento.

Eri beata resta fuoco notti chino gia. Segue altro vi oh umida tu fiume sa. So sa abbassano seguitare importuna monumento. Puo pie accanto nel confido rivolto vedrete sue. Convertira se incontrati emergevano ha un ah coraggiose. Adorarti or torcesse me ambascia veemenza un presenta affinita. Ape sua mie apparve mettere disceso materia volonta rilievo the. Ben bisognava tenerezza fra ora sollevera mai. Bene ho doni un temo ti fine avro so.

Faro dov nell san alle. Pur ali lievissimo sui affaticato incertezza visitatore nei confusione stanchezza. Aspettavo scolpisca titubante mantenere da da fu. Rimase potrei tal nobile chiave doveva stesso tuo. Il da colei oh denti copia casta. Nessuno audacia cio spasimo bisogna col limpida the. Sembro avendo ha me lascia su. El sa scorso audace tu sembra uomini. Ad toccando consunta adorarti entrambi impronta ve sostanza.
"
        };

        string outputPath = Path.Combine(Path.GetTempPath(), "test_output.pdf");

        if (File.Exists(outputPath))
            File.Delete(outputPath);

        // Act
        var pdfBytes = await service.GenerateTherapyPdfAsync(therapy);

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000, "PDF should not be empty or too small");

        // Write PDF to inspect manually
        await File.WriteAllBytesAsync(outputPath, pdfBytes);

        Assert.True(File.Exists(outputPath), "PDF file was not created");
        Assert.True(new FileInfo(outputPath).Length > 1000, "Generated PDF file is unexpectedly small");

        // For debugging
        testOutputHelper.WriteLine($"PDF generated at: {outputPath}");
    }
}