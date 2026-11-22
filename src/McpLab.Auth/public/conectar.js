// Botões "Copiar" da página de instruções. A página funciona sem este script.
async function copy(button, text, fallback) {
  try {
    await navigator.clipboard.writeText(text);
    const label = button.textContent;
    button.textContent = "Copiado";
    setTimeout(() => { button.textContent = label; }, 2000);
  } catch {
    getSelection().selectAllChildren(fallback);
  }
}

document.querySelectorAll("[data-copy]").forEach((button) => {
  button.hidden = false;
  button.addEventListener("click", () => copy(button, button.dataset.copy, button.previousElementSibling));
});

document.querySelectorAll("[data-copy-from]").forEach((button) => {
  const source = document.getElementById(button.dataset.copyFrom);
  button.hidden = false;
  button.addEventListener("click", () => copy(button, source.textContent.trim(), source));
});
