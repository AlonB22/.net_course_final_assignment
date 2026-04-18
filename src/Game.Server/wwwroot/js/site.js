(() => {
  function initRegistrationForm() {
    const form = document.querySelector('[data-registration-form]');
    if (!form) {
      return;
    }

    const countSelect = form.querySelector('[data-participant-count]');
    const countLabel = form.querySelector('[data-participant-count-label]');
    const rows = Array.from(form.querySelectorAll('[data-participant-row]'));

    const syncRows = () => {
      const selectedCount = Number.parseInt(countSelect?.value ?? '0', 10) || 0;

      if (countLabel) {
        countLabel.textContent = String(selectedCount);
      }

      rows.forEach((row, index) => {
        const isVisible = index < selectedCount;
        row.classList.toggle('d-none', !isVisible);

        row.querySelectorAll('input, select, textarea').forEach(field => {
          field.disabled = !isVisible;
        });
      });
    };

    countSelect?.addEventListener('change', syncRows);
    syncRows();
  }

  async function copyValue(button) {
    const value = button?.getAttribute('data-copy-value');
    if (!value) {
      return;
    }

    try {
      await navigator.clipboard.writeText(value);
      button.classList.add('disabled');
      button.textContent = 'Copied';
      window.setTimeout(() => {
        button.textContent = 'Copy session id';
        button.classList.remove('disabled');
      }, 1200);
    } catch {
      const helper = document.createElement('textarea');
      helper.value = value;
      helper.setAttribute('readonly', 'readonly');
      helper.style.position = 'absolute';
      helper.style.left = '-9999px';
      document.body.appendChild(helper);
      helper.select();
      document.execCommand('copy');
      document.body.removeChild(helper);
    }
  }

  document.addEventListener('DOMContentLoaded', () => {
    initRegistrationForm();

    document.querySelectorAll('[data-copy-button]').forEach(button => {
      button.addEventListener('click', () => copyValue(button));
    });
  });
})();
