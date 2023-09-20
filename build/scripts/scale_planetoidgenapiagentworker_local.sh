#!/bin/bash

if [ "$#" -ne 1 ]; then
  echo "Usage: $0 <expected_replica_num>"
  exit 1
fi

if ! [[ $1 =~ ^-?[0-9]+$ ]]; then
  echo "Error: 'expected_replica_num' must be an integer"
  exit 1
fi

if [ "$1" -lt 0 ]; then
  echo "Error: 'expected_replica_num' must be greater than or equal to 0"
  exit 1
fi

echo -e "expected_replica_num = $1"
kubectl scale --replicas=$1 deployment planetoidgenapiagentworker -n planetoidgen

echo -e "\n"
kubectl get pods -n planetoidgen -l app=planetoidgenapiagentworker
