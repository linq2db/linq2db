export default {
    iconLinks: [
        {
            icon: 'github',
            href: 'https://github.com/linq2db/linq2db',
            title: 'Linq To DB'
        }
    ],
    start: () => {
        // Copy button for the install command on the landing page
        const copyIcon = '<i class="bi bi-clipboard" aria-hidden="true"></i>';
        const doneIcon = '<i class="bi bi-check2" aria-hidden="true"></i>';

        for (const box of document.querySelectorAll('.landing-install')) {
            const code = box.querySelector('code');
            if (!code || !navigator.clipboard)
                continue;

            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'landing-copy';
            button.title = 'Copy';
            button.setAttribute('aria-label', 'Copy the install command');
            button.innerHTML = copyIcon;

            button.addEventListener('click', async () => {
                await navigator.clipboard.writeText(code.textContent.trim());
                button.innerHTML = doneIcon;
                button.setAttribute('aria-label', 'Copied');
                setTimeout(() => {
                    button.innerHTML = copyIcon;
                    button.setAttribute('aria-label', 'Copy the install command');
                }, 2000);
            });

            box.appendChild(button);
        }
    }
}
