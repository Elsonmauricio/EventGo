<<<<<<< HEAD
<<<<<<< HEAD
=======
>>>>>>> 333c2090e953223d18414127eea4a5c75613f002
﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventGo.Models
{
    public class Participante
    {
        public int Id { get; set; }

        [Required]
        public string Nome { get; set; } = "";

        public string Email { get; set; } = "";

        // Relações
        public List<Inscricao>? Inscricoes { get; set; }
    }
<<<<<<< HEAD
}
=======
﻿namespace EventGo.Models
{
    public class Participante
    {
    }
}
>>>>>>> 7bd6faf (models e dbcontext)
=======
}
>>>>>>> 333c2090e953223d18414127eea4a5c75613f002
