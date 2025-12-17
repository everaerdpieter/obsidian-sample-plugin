import { Plugin } from 'obsidian';
import mermaid from 'mermaid';

export default class MermaidDiagramPlugin extends Plugin {
	async onload() {
		mermaid.initialize({
			startOnLoad: false,
			theme: 'default',
			securityLevel: 'loose',
		});

		this.registerMarkdownCodeBlockProcessor('mermaid', async (source, el, ctx) => {
			const container = el.createDiv({ cls: 'mermaid-diagram' });
			try {
				const id = `mermaid-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
				const { svg } = await mermaid.render(id, source);
				container.innerHTML = svg;
			} catch (error) {
				container.addClass('mermaid-error');
				container.innerHTML = `<strong>Mermaid Error:</strong><pre>${String(error)}</pre>`;
			}
		});
	}
}
