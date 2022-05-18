# buildr
`buildr` is a multithreaded .NET application for streamlining multi-project, interdependent build processes. `buildr` was originally written for Angular applications, but should be able to accommodate a project in any language.

## Features
 - Incremental building: `buildr` will not recompile projects that haven't changed.
   - `buildr` will recursively register changes from dependencies for "application" level projects only.
 - Parallel build execution

## Usage

### CLI
 1. Drop the `buildr` application directory into an easily accessible folder on your development machine. Anywhere under your `C:` drive is fine.
 2. Add this directory to your `PATH`.
 3. Open your project root folder in a `cmd` or `bash` prompt and enter `buildr start` to start the app. Elevated permissions are not a bad idea here.
 4. The `buildr` Node process should now be running in the background, watching your directory for changes.
    - **Note**: `buildr` will check the directory for changes on startup, so incremental builds work without the `start` command running in the background.
 5. Commands:
    - `start`:
      - Starts file watching process and awaits further CLI input.
    - `stop`:
      - Destroys all child processes and exits `buildr` process.
    - `build [project]`:
      - Queues a project and any dependencies for compilation.
	  - Pass `all` as the project name to build all application level projects.
	  - Flags:
        - `--incremental`: `boolean`
          - If `true`, `buildr` won't build projects that haven't changed since its last recorded build.
          - Default: `true`
        - `--output`: `boolean`
          - If `true`, `buildr` redirects all standard out messages from child processes to the terminal.
          - Default: `false`
		- `--delay`: `int`
		  - The amount of milliseconds `buildr` delays between initiating each concurrent build, in order to not overload `ngcc`.
		  - Default: `500`
    - `reset [project?]`
      - Removes change and build history from `.buildrstate` for a specified project. If no specified project, `buildr` removes all history.
    - `cls`
      - Clears terminal output.
    - `config`
      - Set configuration values from the CLI.
      - Flags:
        - `--terminal`: `string`
          - Sets the desired command prompt to generate child processes. Accepts `bash` or `cmd.exe`
          - Default: `cmd.exe`
        - `--concurrency`: `int`
          - Sets the maximum number of projects `buildr` will run in parallel. Bound between `0` and the number of CPU cores on the machine.
		  - Default: `5`
    - `version`
      - Returns the currently installed `buildr` version

#### Recommended Bash Settings
```
export NODE_OPTIONS=--max-old-space-size=8192
```

**Note**: The `buildr` CLI is targeted to `win-x64` and as such does not always play nice in a `bash` terminal. Use `cmd` prompt for best results.

### VS Code Extension
The `buildr` VS Code Extension exposes shortcuts in the VS Code Command Palette to invoke the `buildr` CLI. For best results, set your default shell profile to `"Command Prompt"` in your VS Code settings.

 1. Navigate to the Extensions tab in VS Code.
 2. Select "Install from VSIX" from the menu in the top right corner of the Extensions tab.
 3. Select the VSIX file from the `buildr` application directory.
 4. Upon activation, run `buildr` CLI commands directly from the VS Code Command Palette.
