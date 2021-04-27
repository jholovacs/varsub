# varsub
.NET Core 3.1/ 5.0 global tool to perform variable substitution on a json file (for appsettings.json etc.)

# Why I Made This
YAML seems to be taking over devops, but there are some nice features in .NET Core that I think are being ignored. I find that a settings file that is updated during the deployment process to be more secure and intuitive than putting all the settings into the YAML or associated script files, but few implementations of YAML support dynamically loading environment-specific settings directly.  I've enjoyed Octopus Deploy, and Azure Pipelines, which have a variable substitution mechanism that's quite handy, but some of the newer, more "barebones" deployment tools (looking at you, AWS CodeDeploy!) do not.  

Your only option for these types of systems is to either include environment-specific settings in the yml file itself (things like production database credentials and other unmentionables), or script out some way of capturing a secure parameter in the devops system of your choice and manually replacing the value in the settings file. I really didn't like either option.

So, I built a simple tool that would allow for simple variable substitution in a JSON file, based upon that secure parameter store.

# To Install
From the command line, run:
`dotnet tool install --global varsub`
NOTE: if you have a private authenticated repository you may need to run this as such:
`dotnet tool update --global --ignore-failed-sources varsub`

# To Use
As of right now, this will only substitute one value at a time. If I get enough interest in this, I may try to read from environment variables or even hook into specific devops environments (AWS CodeDeploy, I'm still looking at you!)

Using the variable substitution tool is pretty simple. Run from the command line as part of your yaml script, you'd first install the tool as seen above, then run the command as  follows:

`varsub --file appsettings.json --parameter-path "ConnectionStrings.ApplicationDb" --value "${{secrets.AppConnectionString}}"`

Assuming your settings file is in the current directory and named "appsettings.json", the tool will load the JSON file, navigate to the node ConnectionStrings.ApplicationDb, and replace the existing value with the value rendered by the `${{secrets.AppConnectionString}}` syntax. Note that the syntax for injecting parameterized values can vary based upon the OS of the build server and the type of devops system you are using.

# Notes
* Variable substitution will not *add* values to the JSON file. The setting must already exist in the file.
* If the JSON file has a raw, unquoted value (for example, a number or boolean) at the designated parameter path, and the replacement value is also of the same type, the value will likewise not be quoted. This is *only* for simple types; any string like JavaScript or JSON will be treated as a string value to prevent any weird sort of injection risks.
* I don't plan on doing much more work with this. If there's an obvious problem I may try to fix it, but the code is available for all to see. Feel free to fix it yourself and send me a PR.
