import { Plugin } from 'obsidian';
import mermaid from 'mermaid';

export default class MermaidDiagramPlugin extends Plugin {
	async onload() {
		mermaid.initialize({
			startOnLoad: false,
			theme: 'default',
			securityLevel: 'loose',
		});

		this.registerMarkdownPostProcessor(async (el, ctx) => {
			// Get the raw markdown source for this section
			const info = ctx.getSectionInfo(el);
			if (!info) return;

			const lines = info.text.split('\n').slice(info.lineStart, info.lineEnd + 1);
			const sectionText = lines.join('\n');

			// Check for :::mermaid blocks
			const match = sectionText.match(/^:::mermaid\s*\n([\s\S]*?)\n:::$/m);
			if (!match) return;

			const mermaidCode = match[1].trim();

			// Clear existing content and render mermaid
			el.empty();
			const container = el.createDiv({ cls: 'mermaid-diagram' });

			try {
				const id = `mermaid-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
				const { svg } = await mermaid.render(id, mermaidCode);
				container.innerHTML = svg;
			} catch (error) {
				container.addClass('mermaid-error');
				container.innerHTML = `<strong>Mermaid Error:</strong><pre>${String(error)}</pre>`;
			}
		});
	}
}
