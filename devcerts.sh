mkcert -install
mkdir devcerts
mkcert -key-file devcerts/carsties.com.key -cert-file devcerts/carsties.com.crt app.carsties.com api.carsties.com id.carsties.com
