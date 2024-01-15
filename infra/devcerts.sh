mkcert -install
mkdir devcerts
mkcert -key-file devcerts/server.key -cert-file devcerts/server.crt app.carsties.com api.carsties.com id.carsties.com
cd devcerts
kubectl delete secret carsties-app-tls
kubectl create secret tls carsties-app-tls --key server.key --cert server.crt

# kubectl create secret generic <name> --from-literal=<key>=<value>