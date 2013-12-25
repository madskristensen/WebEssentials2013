this.isAvailable = (type, value) ->
  deferred = $q.defer()

  $http.get(prApiRoot + "users/available?" + type + "=" + value)
      .success((data) ->
          deferred.resolve(data)
          return
      ).error((data, status) ->
          deferred.reject({ data: data, status: status})
          return
      )
  return deferred.promise