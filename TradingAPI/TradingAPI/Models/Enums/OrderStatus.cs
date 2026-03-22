namespace TradingAPI.Models.Enums
{
    public enum OrderStatus
    {
        Pending,          // În așteptare (încă nu s-a găsit vânzător/cumpărător)
        PartiallyFilled,  // Executat parțial (ex: a cerut 100, a primit 40)
        Filled,           // Executat complet (a primit toate cele 100)
        Cancelled,        // Anulat de utilizator înainte să se execute
        Rejected          // Respins de sistem (ex: fonduri insuficiente pe parcurs)
    }
}