import { ExtensionContext } from 'vscode';
import { buildr } from "./app";

let app: buildr;

export function activate(context: ExtensionContext) {
	try {
		app = new buildr();
		context.subscriptions.push(...app.generateCommands());
	} catch (_err) {
		console.error(_err);
	}
}

export function deactivate() {
	if (app) {
		app.stop();
		if (app.terminal) {
			app.terminal.dispose();
		}
	}
}
