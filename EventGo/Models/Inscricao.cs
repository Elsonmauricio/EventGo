using System.ComponentModel.DataAnnotations;

namespace EventGo.Models
{
    public class Inscricao
    {
        public int Id { get; set; }

        public int EventoId { get; set; }
        public Evento? Evento { get; set; }

        public int ParticipanteId { get; set; }
        public Participante? Participante { get; set; }

        public DateTime DataInscricao { get; set; } = DateTime.Now;
    }
}