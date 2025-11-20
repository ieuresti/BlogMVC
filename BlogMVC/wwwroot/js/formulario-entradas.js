const quill = new Quill('#editor', {
    modules: {
        toolbar: [
            [{ header: [1, 2, false] }],
            ['bold', 'italic', 'underline'],
            ['code-block'],
        ],
    },
    placeholder: 'Coloque aqui la entrada...',
    theme: 'snow', // or 'bubble'
});

function btnEnviarClick() {
    let esValido = validarFormularioCompleto();
    if (!esValido) {
        return;
    }
    // Obtener el contenido de Quill y convertirlo a JSON
    const delta = quill.getContents();
    const deltaJSON = JSON.stringify(delta.ops);
    // Asignar el contenido JSON al campo oculto
    $("#Cuerpo").val(deltaJSON);
    $("#formEntrada").trigger("submit");
}

function validarFormularioCompleto() {
    let formEntradaEsValido = $("#formEntrada").valid();
    let cuerpoEsValido = validarCuerpo();

    return formEntradaEsValido && cuerpoEsValido;
}

function validarCuerpo() {
    let mensajeDeError = null;
    let esValido = true;
    const contenido = quill.getSemanticHTML();

    if (contenido === '<p></p>') {
        mensajeDeError = "El cuerpo es requerido";
        esValido = false;
    }

    $("#cuerpo-error").html(mensajeDeError);
    return esValido;
}

quill.on('text-change', function (delta, oldDelta, source) {
    validarCuerpo();
})

function mostrarPrevisualizacion(event) {
    const input = event.target; // Obtener el input file
    const imagenPreview = document.getElementById('PreviewImagen');
    if (input.files && input.files[0]) {
        // Se crea una URL para la img seleccionada
        const urlImagen = URL.createObjectURL(input.files[0]);
        imagenPreview.src = urlImagen; // Se asigna la URL al src de la img
        imagenPreview.style.display = 'block'; // Se muestra la img
    }
}