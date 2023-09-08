rem @start d:\portable\ngrok\ngrok.exe start --config ngrok.yml site admin
rem @start https://physically-polite-loon.ngrok-free.app/fav?id=2

@start http://localhost:80/fav?id=2
rem @start http://localhost:4040

@call d:\my\work\IISExpress\iisexpress.exe /config:d:\my\work\fb2kindle\Jail_express.config

rem pause