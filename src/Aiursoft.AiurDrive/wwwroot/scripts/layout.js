// noinspection JSJQueryEfficiency

import { autoTheme } from "/node_modules/@aiursoft/autodark.js/dist/esm/autodark.js";
autoTheme();

$('[data-toggle="tooltip"]').tooltip();
$('[data-toggle="tooltip"]').on('click', function () {
    setTimeout(function () {
        $('[data-toggle="tooltip"]').tooltip('hide');
    }, 2000);
});