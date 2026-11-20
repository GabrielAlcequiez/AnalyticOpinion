using CsvHelper.Configuration;

namespace AnalyticOpinion.Data.Extractors.Csv;

public sealed class SurveyCsvRecord
{
    public string? IdOpinion { get; set; }
    public string? IdCliente { get; set; }
    public string? IdProducto { get; set; }
    public string? Fecha { get; set; }
    public string? Comentario { get; set; }
    public string? Clasificacion { get; set; }
    public string? PuntajeSatisfaccion { get; set; }
    public string? Fuente { get; set; }
}

// para manejar que acepte lso encabezados con tildes y sin tildes
public sealed class SurveyCsvMap : ClassMap<SurveyCsvRecord>
{
    public SurveyCsvMap()
    {
        Map(m => m.IdOpinion).Name("IdOpinion");
        Map(m => m.IdCliente).Name("IdCliente").Optional();
        Map(m => m.IdProducto).Name("IdProducto").Optional();
        Map(m => m.Fecha).Name("Fecha");
        Map(m => m.Comentario).Name("Comentario");
        Map(m => m.Clasificacion).Name("Clasificación", "Clasificacion").Optional();
        Map(m => m.PuntajeSatisfaccion).Name("PuntajeSatisfacción", "PuntajeSatisfaccion").Optional();
        Map(m => m.Fuente).Name("Fuente").Optional();
    }
}
