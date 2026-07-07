document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('form[data-confirm]').forEach((form) => {
        form.addEventListener('submit', (event) => {
            const message = form.getAttribute('data-confirm');
            if (message && !window.confirm(message)) {
                event.preventDefault();
            }
        });
    });

    document.querySelectorAll('[data-checkpoint-outcome]').forEach((button) => {
        button.addEventListener('click', () => {
            const target = button.getAttribute('data-bs-target');
            if (!target) {
                return;
            }

            const modal = document.querySelector(target);
            if (!modal) {
                return;
            }

            const outcomeInput = modal.querySelector('[name="Outcome"]');
            if (outcomeInput) {
                outcomeInput.value = button.getAttribute('data-checkpoint-outcome') ?? '';
            }

            const commentInput = modal.querySelector('[name="Comment"]');
            if (commentInput) {
                const commentRequired = button.getAttribute('data-comment-required') === 'true';
                commentInput.required = commentRequired;
                commentInput.value = '';
            }

            const title = modal.querySelector('[data-modal-outcome-title]');
            if (title) {
                title.textContent = button.getAttribute('data-modal-title') ?? '';
            }

            const fileInput = modal.querySelector('[name="Document"]');
            if (fileInput) {
                const documentRequired = button.getAttribute('data-document-required') === 'true';
                fileInput.required = documentRequired;
                fileInput.value = '';
            }
        });
    });
});
