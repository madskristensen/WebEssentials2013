"use strict";

module.exports = {
    reporter: function (res) {
        var messageItems = [];

        res.forEach(function (result) {
            var error = result.error;

            messageItems.push(
                                  {
                                      Line: parseInt(error.line, 10)
                                    , Column: parseInt(error.character, 10)
                                    , Message: error.reason
                                    , Code: error.code
                                  }
                             );
        });

        process.stdout.write(JSON.stringify(messageItems));
    }
};