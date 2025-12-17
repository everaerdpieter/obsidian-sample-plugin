import { Plugin, MarkdownPostProcessorContext } from 'obsidian';
import mermaid from 'mermaid';

export default class MermaidDiagramPlugin extends Plugin {
	private mermaidInitialized = false;

	async onload() {
		this.initializeMermaid();
		this.registerMarkdownPostProcessor(this.processMermaidBlocks.bind(this));
	}

	private initializeMermaid() {
		if (this.mermaidInitialized) return;

		mermaid.initialize({
			startOnLoad: false,
			theme: 'default',
			securityLevel: 'loose',
			flowchart: {
				useMaxWidth: true,
				htmlLabels: true
			}
		});
		this.mermaidInitialized = true;
	}

	private async processMermaidBlocks(el: HTMLElement, ctx: MarkdownPostProcessorContext) {
		const mermaidPattern = /^:::mermaid\s*\n([\s\S]*?)\n:::$/gm;

		const processNode = async (node: Node) => {
			if (node.nodeType === Node.TEXT_NODE) {
				const text = node.textContent || '';
				if (text.includes(':::mermaid')) {
					const parent = node.parentElement;
					if (parent) {
						const fullText = parent.innerHTML;
						const matches = [...fullText.matchAll(mermaidPattern)];

						if (matches.length > 0) {
							let newHtml = fullText;
							for (const match of matches) {
								const mermaidCode = match[1].trim();
								const rendered = await this.renderMermaid(mermaidCode);
								newHtml = newHtml.replace(match[0], rendered);
							}
							parent.innerHTML = newHtml;
						}
					}
				}
			} else if (node.nodeType === Node.ELEMENT_NODE) {
				const element = node as HTMLElement;
				const html = element.innerHTML;

				if (html.includes(':::mermaid')) {
					const matches = [...html.matchAll(mermaidPattern)];

					if (matches.length > 0) {
						let newHtml = html;
						for (const match of matches) {
							const mermaidCode = match[1].trim();
							const rendered = await this.renderMermaid(mermaidCode);
							newHtml = newHtml.replace(match[0], rendered);
						}
						element.innerHTML = newHtml;
						return;
					}
				}

				for (const child of Array.from(node.childNodes)) {
					await processNode(child);
				}
			}
		};

		// Check if the element contains any mermaid blocks
		const fullHtml = el.innerHTML;
		if (fullHtml.includes(':::mermaid')) {
			const matches = [...fullHtml.matchAll(mermaidPattern)];

			if (matches.length > 0) {
				let newHtml = fullHtml;
				for (const match of matches) {
					const mermaidCode = match[1].trim();
					const rendered = await this.renderMermaid(mermaidCode);
					newHtml = newHtml.replace(match[0], rendered);
				}
				el.innerHTML = newHtml;
			}
		}

		// Also handle case where :::mermaid is split across elements
		const paragraphs = el.querySelectorAll('p');
		for (const p of Array.from(paragraphs)) {
			const text = p.textContent || '';
			if (text.includes(':::mermaid') && text.includes(':::')) {
				const match = text.match(/:::mermaid\s*\n?([\s\S]*?)\n?:::/);
				if (match) {
					const mermaidCode = match[1].trim();
					const rendered = await this.renderMermaid(mermaidCode);
					const container = document.createElement('div');
					container.innerHTML = rendered;
					p.replaceWith(container);
				}
			}
		}
	}

	private async renderMermaid(code: string): Promise<string> {
		try {
			const id = `mermaid-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
			const { svg } = await mermaid.render(id, code);
			return `<div class="mermaid-diagram">${svg}</div>`;
		} catch (error) {
			console.error('Mermaid rendering error:', error);
			return `<div class="mermaid-error">
				<strong>Mermaid Error:</strong>
				<pre>${this.escapeHtml(String(error))}</pre>
				<details>
					<summary>Source</summary>
					<pre>${this.escapeHtml(code)}</pre>
				</details>
			</div>`;
		}
	}

	private escapeHtml(text: string): string {
		const div = document.createElement('div');
		div.textContent = text;
		return div.innerHTML;
	}

	onunload() {
		// Cleanup if needed
	}
}
