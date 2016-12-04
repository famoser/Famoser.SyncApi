<?php
/**
 * Created by PhpStorm.
 * User: famoser
 * Date: 28/11/2016
 * Time: 17:03
 */

namespace Famoser\SyncApi\Tests\ControllerTests;

use Famoser\SyncApi\Models\Communication\Entities\DeviceCommunicationEntity;
use Famoser\SyncApi\Models\Communication\Entities\UserCommunicationEntity;
use Famoser\SyncApi\Models\Communication\Request\AuthorizationRequest;
use Famoser\SyncApi\Models\Communication\Request\SyncEntityRequest;
use Famoser\SyncApi\SyncApiApp;
use Famoser\SyncApi\Tests\SampleGenerator;
use Famoser\SyncApi\Tests\TestHelper;
use Famoser\SyncApi\Types\OnlineAction;

/**
 * Class AuthorizationControllerTests
 *
 * @package Famoser\SyncApi\Tests\ControllerTests
 */
class AuthorizationControllerTest extends \PHPUnit_Framework_TestCase
{
    /* @var SyncApiApp $app */
    private $app;
    /* @var TestHelper $testHelper */
    private $testHelper;

    /**
     * create the $app and $testHelper
     */
    public function setUp()
    {
        $this->testHelper = new TestHelper();
        $this->app = $this->testHelper->getTestApp();
    }

    /**
     * cleans the test environment
     */
    public function tearDown()
    {
        $this->testHelper->cleanEnvironment();
    }

    /**
     * tries to create a user for a test application
     */
    public function testCreateUser()
    {
        //arrange
        $syncRequest = new AuthorizationRequest();
        $this->testHelper->authorizeRequest($syncRequest);

        //create user
        $user = new UserCommunicationEntity();
        $user->PersonalSeed = 621842297;
        SampleGenerator::createEntity($user);

        $syncRequest->UserEntity = $user;
        $syncRequest->UserId = $user->Id;

        $this->testHelper->mockApiRequest($syncRequest, "auth/sync", $this->app);

        //act
        $response = $this->app->run();

        //assert
        $this->testHelper->checkForSuccessfulApiResponse($this, $response);
    }

    /**
     * tries to create a user for a test application
     */
    public function testCreateDevice()
    {
        //arrange
        $syncRequest = new AuthorizationRequest();
        $this->testHelper->authorizeRequest($syncRequest);

        $device = new DeviceCommunicationEntity();
        SampleGenerator::createEntity($device);

        $syncRequest->DeviceEntity = $device;
        $syncRequest->UserId = $this->testHelper->getUserId();
        $syncRequest->DeviceId = $device->Id;

        $this->testHelper->mockApiRequest($syncRequest, "auth/sync", $this->app);

        //act
        $response = $this->app->run();

        //assert
        $this->testHelper->checkForSuccessfulApiResponse($this, $response);
    }
}