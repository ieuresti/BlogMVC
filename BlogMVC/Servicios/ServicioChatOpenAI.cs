using BlogMVC.Configuraciones;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BlogMVC.Servicios
{
    public class ServicioChatOpenAI: IServicioChat
    {
        private readonly IOptions<ConfiguracionesIA> options;
        private readonly OpenAIClient openAIClient;

        private string systemPromptGenerarCuerpo = """
            Eres un ingeniero de software experto en ASP.NET Core. 
            Escribes artículos con un tono jovial y amigable. 
            Te esfuerzas para que los principiantes entiendan las cosas dando ejemplos prácticos.
            """;

        private string ObtenerPromptGeneraCuerpo(string titulo) => $"""

            Crear un artículo para un blog. El título del artículo será {titulo}.

            Si lo entiendes conveniente, debes insertar tips.

            El formato de respuesta es HTML. Por tanto, debes colocar negritas donde consideres, 
            títulos, subtítulos, entre otras cosas que ayuden a resaltar el formato.

            La respuesta no debe ser un documento HTML, sino solamente el artículo en formato HTML, 
            con sus párrafos bien separados. Por tanto, nada de DOCTYPE, ni head, ni body. Solo el artículo.

            No incluyas el título del artículo en el artículo.

            """;

        public ServicioChatOpenAI(IOptions<ConfiguracionesIA> options, OpenAIClient openAIClient)
        {
            this.options = options;
            this.openAIClient = openAIClient;
        }

        // Método para generar el cuerpo del artículo basado en el titulo proporcionado
        public async Task<string> GenerarCuerpo(string titulo)
        {
            // Obtener el nombre del modelo desde la configuración inyectada.
            var modeloTexto = options.Value.ModeloTexto;
            // Obtener un cliente de chat para el modelo indicado.
            // `openAIClient.GetChatClient` encapsula la comunicación con el API para el modelo dado.
            var clienteChat = openAIClient.GetChatClient(modeloTexto);
            // Construir el mensaje del sistema.
            // El mensaje de sistema establece el rol/comportamiento del asistente (tono, estilo, reglas).
            var mensajeDeSistema = new SystemChatMessage(systemPromptGenerarCuerpo);
            // Construir el prompt que describirá la tarea concreta (generar el artículo).
            // Aquí se utiliza el título para personalizar el prompt.
            var promptUsuario = ObtenerPromptGeneraCuerpo(titulo);
            // Construir el mensaje del usuario con el prompt preparado.
            var mensajeUsuario = new UserChatMessage(promptUsuario);
            // Agrupar los mensajes en el orden correcto: primero el sistema, luego el usuario.
            ChatMessage[] mensajes = { mensajeDeSistema, mensajeUsuario };
            // Enviar los mensajes al modelo y esperar la respuesta completa.
            // `CompleteChatAsync` realiza la petición al API y devuelve una estructura con el contenido.
            var respuesta = await clienteChat.CompleteChatAsync(mensajes);
            // Extraer el texto de la primera parte del contenido devuelto.
            // La API puede devolver varios elementos en `Content`; aquí se toma el primero.
            var cuerpo = respuesta.Value.Content[0].Text;
            // Retornar el HTML generado por el modelo.
            return cuerpo;
        }

        // Método para generar el cuerpo del artículo en un flujo (streaming) basado en el título proporcionado
        public async IAsyncEnumerable<string> GenerarCuerpoStream(string titulo)
        {
            // Obtener modelo y cliente
            var modeloTexto = options.Value.ModeloTexto;
            var clienteChat = openAIClient.GetChatClient(modeloTexto);

            // Construir mensajes (sistema + usuario)
            var mensajeDeSistema = new SystemChatMessage(systemPromptGenerarCuerpo);
            var promptUsuario = ObtenerPromptGeneraCuerpo(titulo);
            var mensajeUsuario = new UserChatMessage(promptUsuario);
            ChatMessage[] mensajes = { mensajeDeSistema, mensajeUsuario };

            // Consumir la respuesta en modo streaming
            // `CompleteChatStreamingAsync` devuelve un IAsyncEnumerable de actualizaciones (chunks).
            // Cada `completionUpdate` puede contener uno o varios fragmentos en `ContentUpdate`.
            await foreach (var completionUpdate in clienteChat.CompleteChatStreamingAsync(mensajes))
            {
                // Cada fragmento en `ContentUpdate` representa una pieza incremental del texto.
                // Se itera y se retorna cada `contenido.Text` al consumidor del IAsyncEnumerable.
                foreach (var contenido in completionUpdate.ContentUpdate)
                {
                    // Nota: los fragmentos pueden venir separados arbitrariamente (palabras, signos,
                    // trozos de HTML). El consumidor puede concatenarlos para obtener el resultado final.
                    yield return contenido.Text;
                }
            }
            // Cuando el foreach termina, el stream se considera completado.
        }
    }
}
