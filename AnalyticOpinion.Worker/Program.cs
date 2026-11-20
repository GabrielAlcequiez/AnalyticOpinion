using AnalyticOpinion.Data;
using AnalyticOpinion.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddExtraction();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

// En desarrollo la consola queda abierta para revisar el resumen; en producción el proceso
// termina solo para que el programador de tareas reciba el ExitCode.
if (builder.Environment.IsDevelopment() && !Console.IsInputRedirected)
{
    Console.WriteLine();
    Console.WriteLine("Presione cualquier tecla para cerrar...");
    Console.ReadKey(intercept: true);
}
