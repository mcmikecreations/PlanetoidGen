kubectl delete namespace planetoidgen
kubectl delete namespace messaging
kubectl delete namespace mongo
kubectl delete namespace postgres

if [ "$1" == '--all' ]; then
    kubectl delete namespace overpass
fi