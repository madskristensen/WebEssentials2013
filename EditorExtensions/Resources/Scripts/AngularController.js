/* globals angular */

(function () {{
    'use strict';

    // Controller name is handy for logging
    var controllerId = '{0}';

    // Define the controller on the module.
    // Inject the dependencies. 
    // Point to the controller definition function.
    angular.module('app').controller(controllerId, ['$scope', {0}]);

    function {0}($scope) {{
        // Using 'Controller As' syntax, so we assign this to the vm variable (for viewmodel).
        var vm = this;

        // Bindable properties and functions are placed on vm.
        vm.activate = activate;
        vm.title = '{0}';

        function activate() {{
        }}

        //#region Internal Methods        

        //#endregion
    }}
}})();