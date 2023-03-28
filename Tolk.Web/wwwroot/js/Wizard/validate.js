$.fn.extend({

    wizardFormValidation: function (opts) {
        var $wizard = $(this);
        var options = $.extend({
            validationFalseHandler: function (validator) {
                console.log(validator);
                return;
            }
        },
            opts);
        var wizardOpts = $.extend(opts, {

            onloadHandler: function () {
                if (!($wizard.closest('form').length === 1 && $wizard.find('form').length === 0)) {
                    throw "There are no form tags or they are missplaced.";
                }
                var i = 0;
                $wizard.find(".wizard-breadcrumb-button").each(function () {
                    if (i !== 0) {
                        $(this).data("notClickable", true);
                    }
                    i++;
                });
                if (options.onloadHandler !== undefined) {
                        options.onloadHandler();
                }
            },
            nextHandler: async function (event) {
                var $form = $wizard.closest('form');
                var currentStep = $(".wizard").data("currentStep");
                var $noValidation = $wizard.find(".wizard-step:not(.wizard-step-hidden)").eq(currentStep)
                           .hasClass("wizard-no-forward-validation");
                if (!$noValidation) {
                    var validator = $form.validate();
                    var validateResult = $form.valid();
                    if (!validateResult) {
                        options.validationFalseHandler(validator);
                        return false;
                    }
                }
                if (options.nextHandler !== undefined) {
                    var result = await options.nextHandler(event);
                    if (result === false) {
                        return false;
                    }
                }
                $wizard.find(".wizard-breadcrumb-button").eq(currentStep + 1).data("notClickable", false);
                $form.find(".field-validation-error")
                    .addClass("field-validation-valid")
                    .removeClass("field-validation-error");
                $form.find(".input-validation-error")
                    .removeClass("input-validation-error");
                $form.find(".at-least-one-error")
                    .removeClass("at-least-one-error");
            }
        });
        return $(this).wizard(wizardOpts);
    }
});