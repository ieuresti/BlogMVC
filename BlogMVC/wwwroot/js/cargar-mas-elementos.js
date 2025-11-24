// Funcion para observar un elemento y notificar a Blazor cuando entra en el view port
// idElemento: ID del elemento a observar
// dotNetHelper: Referencia al objeto .NET para invocar metodos desde JS
window.observarElemento = (idElemento, dotNetHelper) => {
    // Crear un observador que detecta cuando el elemento entra en el view port
    let observador = new IntersectionObserver((entradas) => {
        // Si el elemento es visible, invocar el metodo en Blazor
        if (entradas[0].isIntersecting) {
            dotNetHelper.invokeMethodAsync("CargarMasElementos");
        }
    });
    // Obtener el elemento por su ID
    let elemento = document.getElementById(idElemento);
    // Si el elemento existe, comenzar a observarlo
    if (elemento) {
        observador.observe(elemento);
    }
}