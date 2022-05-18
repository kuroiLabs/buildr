import * as vscode from "vscode";

export class buildr {

	public terminal!: vscode.Terminal | null;

	private _projectName: string = "";

	private _running: boolean = false;

	private get root(): string {
		if (vscode.workspace?.workspaceFolders && vscode.workspace?.workspaceFolders[0])
			return vscode.workspace.workspaceFolders[0].uri.path;

		return "";
	}

	constructor() {
		this._findExistingTerminal();
	}

	public generateCommands(): vscode.Disposable[] {
		return [
			vscode.commands.registerCommand("buildr.start", () => this.start()),
			vscode.commands.registerCommand("buildr.stop", () => this.stop()),
			vscode.commands.registerCommand("buildr.build", () => this.build(true)),
			vscode.commands.registerCommand("buildr.buildfull", () => this.build(false)),
			vscode.commands.registerCommand("buildr.buildall", () => this.buildAll(true)),
			vscode.commands.registerCommand("buildr.buildallfull", () => this.buildAll(false)),
			vscode.commands.registerCommand("buildr.reset", () => this.reset())
		];
	}

	public start(): void {
		if (this._running) {
			vscode.window.showWarningMessage("[buildr] buildr is already running.");
			return;
		}

		this._execute("buildr start");
		this._running = true;
	}

	public stop(): void {
		if (!this._running) {
			vscode.window.showErrorMessage("[buildr] buildr process not running!");
			return;
		}
		this._execute("stop");
		this._running = false;
	}

	public build(_incremental: boolean = true): void {
		if (!this._running) {
			vscode.window.showErrorMessage("[buildr] buildr process not running!");
			return;
		}
		vscode.window.showInputBox({
			value: this._projectName,
			placeHolder: "Enter project name"
		}).then(
			_project => {
				this._projectName = _project || "";

				if (!this._projectName)
					return;

				const _command: string = "build " + this._projectName + (!_incremental ? " --incremental false" : "");
				this._execute(_command);
			},
			_error => {
				vscode.window.showErrorMessage("[buildr] Unexpected error requesting project name: " + _error);
			}
		);
	}

	public buildAll(_incremental: boolean = true): void {
		if (!this._running) {
			vscode.window.showErrorMessage("[buildr] buildr process not running!");
			return;
		}

		const _command: string = "build all " + (!_incremental ? " --incremental false" : "");
		this._execute(_command);
	}

	public reset(): void {
		if (!this._running) {
			vscode.window.showErrorMessage("[buildr] buildr process not running!");
			return;
		}
		this._execute("reset");
	}

	private _findExistingTerminal(): void {
		const _terminal: vscode.Terminal | undefined = vscode.window.terminals.find(
			_terminal => _terminal.name === "buildr");

		this.terminal = _terminal || null;
	}

	private _createTerminal(): void {
		this.terminal = vscode.window.createTerminal({
			name: "buildr"
		});
	}

	private _execute(_command: string): void {
		if (!this.terminal) {
			this._createTerminal();
			vscode.window.onDidCloseTerminal(_terminal => {
				if (_terminal.name === this.terminal?.name) {
					this.terminal = null;
					this._running = false;
				}
			});
		}

		this.terminal?.sendText(_command);
		this.terminal?.show();
	}

}