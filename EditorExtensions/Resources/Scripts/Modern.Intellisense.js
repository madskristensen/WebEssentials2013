/*
 * This file contains addional Intellisense for Visual Studio's JavaScript editor
 */

//#region Shadow DOM

var ShadowRoot = _$inherit(DocumentFragment);

ShadowRoot.prototype.applyAuthorStyles = true;
ShadowRoot.prototype.resetStyleInheritance = true;
ShadowRoot.prototype.activeElement = Element.prototype;
ShadowRoot.prototype.innerHTML = "";
ShadowRoot.prototype.styleSheets = StyleSheetList.prototype;

ShadowRoot.prototype.getElementById = document.getElementById;
ShadowRoot.prototype.getElementsByClassName = document.getElementsByClassName;
ShadowRoot.prototype.getElementsByTagName = document.getElementsByTagName;
ShadowRoot.prototype.getElementsByTagNameNS = document.getElementsByTagNameNS;
ShadowRoot.prototype.getSelection = document.getSelection;
ShadowRoot.prototype.elementFromPoint = document.elementFromPoint;

Element.prototype.createShadowRoot = function () {
    /// <summary>The ShadowRoot object represents the shadow root.</summary>
    /// <returns type="ShadowRoot" />
}

Element.prototype.webkitCreateShadowRoot = Element.prototype.createShadowRoot;

//#endregion

//#region Vibration API

Navigator.prototype.vibrate = function () {
    /// <signature>
    ///   <param name="time" type="Number">The number of miliseconds to vibrate.</param>
    ///   <returns type="Boolean" />
    /// </signature>
    /// <signature>
    ///   <param name="pattern" type="Array">An array of miliseconds that makes up the pattern of vibration.</param>
    ///   <returns type="Boolean" />
    /// </signature>
}

//#endregion

//#region Fullscreen API

Element.prototype.requestFullscreen = Element.prototype.msRequestFullscreen;
Element.prototype.mozRequestFullscreen = Element.prototype.msRequestFullscreen;
Element.prototype.webkitRequestFullscreen = Element.prototype.msRequestFullscreen;

document.fullscreenElement = document.msFullscreenElement;
document.mozFullscreenElement = document.msFullscreenElement;
document.webkitFullscreenElement = document.msFullscreenElement;

document.fullscreenEnabled = document.msFullscreenEnabled;
document.mozFullscreenEnabled = document.msFullscreenEnabled;
document.webkitFullscreenEnabled = document.msFullscreenEnabled;

document.exitFullscreen = document.msExitFullscreen;
document.mozExitFullscreen = document.msExitFullscreen;
document.webkitExitFullscreen = document.msExitFullscreen;

//#endregion

//#region Canvas

Element.prototype.getContext = HTMLCanvasElement.prototype.getContext;

//#endregion

//#region Server-Sent Events

function EventSource(url) {
    /// <signature>
    ///   <param name="url" type="String">An absolute URI.</param>
    ///   <returns type="EventSource" />
    /// </signature>
    /// <signature>
    ///   <param name="url" type="String">An absolute URI.</param>
    ///   <param name="eventSourceInitDict" type="dictionary" />
    ///   <returns type="EventSource" />
    /// </signature>

    this.url = url;
    this.withCredentials = false;
    this.readyState = 0;

    this.close = function () { }


    this.onopen = function (event) { }
    this.onerror = function (event) { }
    this.onmessage = function (event) { }
}

EventSource.CONNECTING = 0;
EventSource.OPEN = 1;
EventSource.CLOSED = 2;

//#endregion

//#region HTML Import

Element.prototype.import = Document.prototype;

//#endregion

//#region Object.observe()

Object.observe = function (object, callback) {
    /// <summary>Oberves changes made to an object</summary>
    /// <param name="object" type="object">The object to observe.</param>
    /// <param name="callback" type="function(changes)">The callback function.</param>
};

//#endregion

//#region Angular.js


(function () {

    if (!angular) return;

    //#region Logging Functions
    function indent(level) {
        var pad = '  '; // Two-space pad.

        return Array(level + 1).join(pad);
    }

    function log(value, filter, key, level) {
        if (angular.isUndefined(value)) {
            value = 'undefined';
        }

        if (angular.isUndefined(filter)) {
            filter = function () { return true; };
        }

        if (angular.isUndefined(key)) {
            key = '';
        }

        if (angular.isUndefined(level)) {
            level = 0;
        }

        var pad = indent(level);

        if (angular.isArray(value)) {
            intellisense.logMessage(pad + (key ? key + ': ' : '') + ' [');

            forEach(value, function (item) {
                log(item, filter, '', level + 1);
            });

            intellisense.logMessage(pad + ']' + (level > 0 ? ',' : ''));
        } else if (angular.isObject(value)) {
            if (filter(value)) {
                intellisense.logMessage(pad + (key ? key + ': ' : '') + '{');

                forEach(value, function (propertyValue, key) {
                    log(propertyValue, filter, key, level + 1);
                });

                intellisense.logMessage(pad + '}' + (level > 0 ? ',' : ''));
            }
        } else if (angular.isFunction(value)) {
            intellisense.logMessage(pad + (key ? key + ': ' : '') + '(Function)' + (level > 0 ? ',' : ''));
        } else {
            if (angular.isString(value)) {
                value = '"' + value + '"';
            }

            intellisense.logMessage(pad + (key ? key + ': ' : '') + value + (level > 0 ? ',' : ''));
        }
    }

    //#endregion

    //#region Utility Functions

    function forEach(obj, iterator, context) {
        var key;
        if (obj.forEach && obj.forEach !== forEach) {
            obj.forEach(iterator, context);
        } else if (angular.isArray(obj)) {
            for (key = 0; key < obj.length; key++) {
                iterator.call(context, obj[key], key);
            }
        } else {
            for (key in obj) {
                iterator.call(context, obj[key], key);
            }
        }

        return obj;
    }

    //#endregion

    //#region Module and Component Tracking

    // Keep track of module names and declared components.
    var moduleNames = [];
    var components = {};

    var componentTypesToTrack = ['provider', 'constant', 'value', 'factory', 'service'];
    var componentTypesWithProviders = ['provider', 'factory', 'service'];
    var moduleProviderFunctions = ['provider', 'factory', 'service', 'animation', 'filter', 'controller', 'directive', 'config', 'run'];

    var providerSuffix = 'Provider';
    var providerSuffixRegex = /Provider$/;

    // Search the window object for globally-defined angular modules, and track them if they are found.
    if (window) {
        forEach(window, function (obj) {
            if (isAngularModule(obj)) {
                trackModule(obj);
            }
        });
    }

    function isAngularModule(obj) {
        // Angular modules all have names and invoke queues and core provider functions.
        return angular.isString(obj.name) &&
            angular.isArray(obj._invokeQueue) &&
            angular.isFunction(obj.provider) &&
            angular.isFunction(obj.constant) &&
            angular.isFunction(obj.value) &&
            angular.isFunction(obj.factory) &&
            angular.isFunction(obj.service);
    }

    // Decorate the angular.module function to record the name of each module as it is registered.
    var originalModuleFunction = angular.module;

    angular.module = function () {
        // Call the original module function.
        var returnValue = originalModuleFunction.apply(angular, arguments);

        // Ensure that the module, its dependencies, and all of their components are tracked.
        trackModule(arguments[0]);

        // Call the configuration function if one is specified.
        if (arguments[2]) {
            returnValue.config(arguments[2]);
        }

        return returnValue;
    };

    function trackModule(moduleOrName) {
        var moduleName, module;

        if (angular.isString(moduleOrName)) {
            // If the argument is a module name, retrieve the module from the angular.module function.
            moduleName = moduleOrName;
            module = originalModuleFunction(moduleName);
        } else {
            // Otherwise the argument is a module, so get the name from its name property.
            module = moduleOrName;
            moduleName = module.name;
        }

        if (moduleNames.indexOf(moduleName) == -1) {
            // Recursively process dependent modules.
            forEach(module.requires, trackModule);

            // Store that the module has been processed to prevent duplicate processing.
            moduleNames.push(moduleName);

            // Track components for the module.
            trackComponents(module);
        }
    }
    function trackComponents(module) {
        // Track all components of the specified types.
        forEach(module._invokeQueue, trackComponent);

        // Initialize each component with empty object dependencies. 
        forEach(moduleProviderFunctions, function (providerFunction) {

            // Decorate the component type function to call component functions with empty object parameters.
            var originalProviderFunction = module[providerFunction];

            // Only decorate the provider function if the module has it (which it may not for animate).
            if (originalProviderFunction) {
                module[providerFunction] = function () {

                    // Call the original component type function.
                    var returnValue = originalProviderFunction.apply(module, arguments);

                    // Initialize components.
                    initializeComponents(providerFunction, module._invokeQueue);

                    return returnValue;
                };
            }
        });
    }
    function trackComponent(component) {
        var type = component[1];

        // Only track the component if it is of a known type.
        if (componentTypesToTrack.indexOf(type) !== -1) {
            var name = component[2][0];

            // Record the component itself.
            components[name] = component;

            // If the component has a provider, record that as well.
            if (componentTypesWithProviders.indexOf(type) !== -1) {
                if (!providerSuffixRegex.test(name)) {
                    components[name + providerSuffix] = component;
                }
            }
        }
    }

    function initializeComponents(providerFunction, comps) {
        var injector = angular.injector(moduleNames);

        forEach(comps, function (comp) {
            // Only initialize the component if it is the appropriate type.
            if (componentMatchesProviderFunction(comp, providerFunction)) {
                initialize(injector, comp[2][1])
            }
        });
    }
    function initialize(injector, initializer) {
        // Find the component dependencies.
        var dependencyNames = injector.annotate(initializer);
        var dependencies = [];

        if (dependencyNames) {
            forEach(dependencyNames, function (dependencyName) {
                dependencies.push(createInstance(injector, dependencyName));
            });
        }

        // Find the component initialization function.
        var initializeFunction;

        if (angular.isFunction(initializer)) {
            initializeFunction = initializer;
        } else {
            forEach(initializer, function (item, index) {
                if (initializer.hasOwnProperty(index) && angular.isFunction(item)) {
                    initializeFunction = item;
                }
            });
        }

        if (initializeFunction) {
            // Call the initialize function with dependency objects (limit to 20 for now).
            initializeFunction(
                dependencies[0],
                dependencies[1],
                dependencies[2],
                dependencies[3],
                dependencies[4],
                dependencies[5],
                dependencies[6],
                dependencies[7],
                dependencies[8],
                dependencies[9],
                dependencies[10],
                dependencies[11],
                dependencies[12],
                dependencies[13],
                dependencies[14],
                dependencies[15],
                dependencies[16],
                dependencies[17],
                dependencies[18],
                dependencies[19]);
        }
    }
    function componentMatchesProviderFunction(component, providerFunction) {
        var componentProvider = component[0];
        var componentProviderFunction = component[1];

        switch (componentProvider) {
            case '$controllerProvider':
                return providerFunction == 'controller';
            case '$filterProvider':
                return providerFunction == 'filter';
            case '$compileProvider':
                return providerFunction == 'directive';
            case '$injector':
                return providerFunction == 'config';
            case '$provide': // for other services
                return providerFunction == componentProviderFunction;
            default:
                return false; // Never try to match unexpected types.
        }
    }
    function createInstance(injector, name) {
        if (injector.has(name)) {
            // If the service is registered with the injector, get it from there.
            return injector.get(name);
        } else if (name == '$scope') {
            // Perform special injection for $scope, since it is a well known object but not available in the injector.
            return injector.get('$rootScope');
        } else {
            var component = components[name];

            if (component[0] == '$provide' && component[1] == 'value') {
                // If the component is a value (registered by angular.mock.module), just return it.
                return component[2][1];
            } else {
                // Invoke the provider function.
                return injector.invoke(injectable);
            }
        }
    }

    // Always add the AngularJS core module.
    trackModule('ng');

    //#endregion

    //#region Jasmine Intellisense

    if (window.jasmine) {
        // Create an array of functions to override.
        var overrides = [
            'describe', 'xdescribe',
            'beforeEach', 'afterEach',
            'it', 'xit',
            'module', 'inject',
            { source: angular.mock, method: 'module' },
            { source: angular.mock, method: 'inject' }
        ];

        var injector = undefined;

        forEach(overrides, function (override) {
            // Extract source and method from the override.
            var source = override.source || window;
            var method = override.method || override;

            // Override the original method.
            var originalMethod = source[method];

            source[method] = function () {
                // Call the original method.
                originalMethod.apply(source, arguments);

                if (method == 'module') {
                    // Track each named module, call each anonymous module, and register components.
                    forEach(arguments, function (argument) {
                        if (angular.isString(argument)) {
                            // Track the module.
                            trackModule(argument);
                        } else if (angular.isFunction(argument) || angular.isArray(argument)) {
                            // TODO: Inject the module config function if it's an array.
                            argument();
                        } else if (angular.isObject(argument)) {
                            angular.forEach(argument, function (value, key) {
                                // Register a value component for each object property (like angular.mock.module does).
                                components[key] = [
                                    '$provide',
                                    'value',
                                    {
                                        0: key,
                                        1: value
                                    }
                                ];
                            });
                        }
                    });

                    // (Re)create an injector for the module.
                    injector = angular.injector(moduleNames);
                } else if (method == 'inject') {
                    // Perform injection on each argument of the method.
                    forEach(arguments, function (argument) {
                        initialize(injector, argument);
                    });
                } else {
                    // Otherwise, call any function arguments to the method.
                    forEach(arguments.filter(angular.isFunction), function (argument) {
                        argument();
                    });
                }
            };
        });
    }

    //#endregion

    intellisense.addEventListener('statementcompletion', function (event) {
        var filterRegex = /^\$\$.*/;

        event.items = event.items.filter(function (item) {
            return !filterRegex.test(item.name);
        });
    });
})();

//#endregion